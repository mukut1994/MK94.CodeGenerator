using MK94.DataGenerator.Generator.Generators;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MK94.DataGenerator.Intermediate.CSharp
{
    public interface IGenerator
    {
        void Generate(CodeBuilder builder);

        void GetRequiredReferences(HashSet<CsTypeReference> refs) { }
    }

    public abstract record CsTypeReference
    {
        public static CsTypeReference ToRaw(string type)
        {
            return new NamedTypeReference(type);
        }

        public static CsTypeReference ToType<T>()
        {
            return new NamedTypeReference(CSharpHelper.CSharpName(typeof(T)));
        }

        public abstract string Resolve(CSharpCodeGenerator root);
    }

    internal record NamedTypeReference : CsTypeReference
    {
        public string Name { get; private init; }

        public NamedTypeReference(string name)
        {
            Name = name;
        }

        public override string Resolve(CSharpCodeGenerator root)
        {
            return Name;
        }
    }

    public class CSharpCodeGenerator
    {
        public Dictionary<string, IntermediateFileDefinition> Files { get; } = new();

        public IntermediateFileDefinition File(string fileName)
        {
            var definition = Files.GetOrAdd(fileName, () => new(this));

            return definition;
        }

        public void Generate(Func<string, CodeBuilder> factory)
        {
            foreach (var file in Files)
            {
                var builder = factory(file.Key);

                file.Value.Generate(builder);

                builder.Flush();
            }
        }
    }

    public class IntermediateFileDefinition : IGenerator
    {
        private CSharpCodeGenerator root { get; }

        public IntermediateFileDefinition(CSharpCodeGenerator root)
        {
            this.root = root;
        }

        public Dictionary<string, IntermediateNamespaceDefintion> Namespaces { get; } = new();

        public IntermediateNamespaceDefintion Namespace(string @namespace)
        {
            var definition = Namespaces.GetOrAdd(@namespace, () => new(root, @namespace));

            return definition;
        }
        public void Generate(CodeBuilder builder)
        {
            foreach (var @namespace in Namespaces)
            {
                @namespace.Value.Generate(builder);
            }
        }

        public class IntermediateNamespaceDefintion : IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public string Namespace { get; }

            public Dictionary<string, IntermediateTypeDefinition> Types { get; } = new();

            public IntermediateNamespaceDefintion(CSharpCodeGenerator root, string @namespace)
            {
                Namespace = @namespace;
                this.root = root;
            }

            public IntermediateTypeDefinition Type(string name, BindingFlags flags)
            {
                var definition = Types.GetOrAdd(name, () => new IntermediateTypeDefinition(root, flags: flags, name: name));

                return definition;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append($"namespace {Namespace}")
                    .WithBlock((b, i) => i.Value.Generate(b), Types);
            }
        }

        public abstract class IntermediateMemberDefinition
        {
            public string Name { get; set; }

            public BindingFlags Flags { get; set; }

            public IntermediateMemberDefinition(BindingFlags flags, string name)
            {
                Name = name;
                Flags = flags;
            }

            public void MemberFlags(CodeBuilder builder)
            {
                if (Flags.HasFlag(BindingFlags.Public))
                    builder.AppendWord("public");

                if (Flags.HasFlag(BindingFlags.Static))
                    builder.AppendWord("static");
            }

            public void MemberName(CodeBuilder builder)
            {
                builder.AppendWord(Name);
            }
        }


        public abstract class IntermediateTypedMemberDefinition : IntermediateMemberDefinition
        {
            public CsTypeReference Type { get; set; }

            protected IntermediateTypedMemberDefinition(BindingFlags flags, CsTypeReference type, string name) : base(flags, name)
            {
                Type = type;
            }
        }

        public class IntermediatePropertyDefinition : IntermediateTypedMemberDefinition, IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public IntermediatePropertyDefinition(CSharpCodeGenerator root, BindingFlags flags, CsTypeReference type, string name) : base(flags, type, name) 
            {
                this.root = root;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append(MemberFlags)
                    .AppendWord(Type.Resolve(root))
                    .Append(MemberName)
                    .AppendLine("{ get; set; }");
            }


            public void GetRequiredReferences(HashSet<CsTypeReference> refs)
            {
                refs.Add(Type);
            }
        }

        public class IntermediateArgumentDefinition : IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public string Name { get; }

            public CsTypeReference Type { get; }

            public IntermediateArgumentDefinition(CSharpCodeGenerator root, CsTypeReference type, string name)
            {
                Name = name;
                Type = type;
                this.root = root;
            }

            public void Generate(CodeBuilder builder)
            {
                builder.AppendWord(Type.Resolve(root)).AppendWord(Name).AppendOptionalComma();
            }
        }

        public class IntermediateMethodDefinition : IntermediateTypedMemberDefinition, IGenerator
        {
            private CSharpCodeGenerator root { get; }
            public MemoryStream BodyStream { get; }
            public CodeBuilder Body { get; }
            public List<IntermediateArgumentDefinition> Arguments { get; } = new();

            public IntermediateMethodDefinition(CSharpCodeGenerator root, BindingFlags flags, CsTypeReference type, string name) : base(flags, type, name)
            {
                Body = CodeBuilder.FromMemoryStream(out var stream);
                BodyStream = stream;
                this.root = root;
            }

            public IntermediateMethodDefinition WithArgument(CsTypeReference type, string name)
            {
                Arguments.Add(new(root, type, name));

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                Body.Flush();
                BodyStream.Flush();
                BodyStream.Position = 0;

                builder
                    .Append(MemberFlags).AppendWord(Type.Resolve(root)).Append(MemberName)
                    .OpenParanthesis()
                        .Append((b, arg) => arg.Generate(b), Arguments)
                    .CloseParanthesis()
                    .WithBlock(b => b.Append(BodyStream))
                    ;
            }
        }

        public class IntermediateTypeDefinition : IntermediateMemberDefinition, IGenerator
        {
            private CSharpCodeGenerator root { get; }
            public Dictionary<string, IntermediatePropertyDefinition> Properties = new();
            public Dictionary<string, IntermediateMethodDefinition> Methods = new();

            public IntermediateTypeDefinition(CSharpCodeGenerator root, BindingFlags flags, string name) : base(flags, name)
            {
                this.root = root;
            }

            public IntermediatePropertyDefinition Property(BindingFlags flags, CsTypeReference type, string name)
            {
                var definition = Properties.GetOrAdd(name, () => new(root, flags, type, name));

                return definition;
            }

            public IntermediateMethodDefinition Method(BindingFlags flags, CsTypeReference type, string name)
            {
                var definition = Methods.GetOrAdd(name, () => new(root, flags, type, name));

                return definition;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append(MemberFlags)
                    .AppendWord("class")
                    .Append(MemberName)
                    .OpenBlock()
                        .Append((b, p) => p.Value.Generate(b), Properties)
                        .NewLine()
                        .Append((b, p) => p.Value.Generate(b), Methods)
                    .CloseBlock();
            }
        }
    }
}
