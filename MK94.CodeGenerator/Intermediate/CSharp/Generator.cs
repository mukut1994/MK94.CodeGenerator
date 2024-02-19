using MK94.CodeGenerator.Generator.Generators;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp
{
    public interface IGenerator
    {
        void Generate(CodeBuilder builder);

        void GetRequiredReferences(HashSet<CsharpTypeReference> refs) { }
    }

    public abstract record CsharpTypeReference
    {
        public static CsharpTypeReference ToRaw(string type)
        {
            return new NamedTypeReference(type);
        }

        public static CsharpTypeReference ToVoid() => ToRaw("void");

        public static CsharpTypeReference ToType<T>()
        {
            return new NamedTypeReference(MK94.CodeGenerator.Generator.CSharpHelper.CSharpName(typeof(T)));
        }

        public static CsharpTypeReference ToType(Type t)
        {
            return new NamedTypeReference(MK94.CodeGenerator.Generator.CSharpHelper.CSharpName(t));
        }

        public abstract string Resolve(CSharpCodeGenerator root);
    }

    internal record NamedTypeReference : CsharpTypeReference
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

    public class CSharpCodeGenerator : IFileGenerator
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
        public Dictionary<string, IntermediateNamespaceDefintion> Namespaces { get; } = new();

        public HashSet<string> Usings { get; } = new();

        public IntermediateFileDefinition(CSharpCodeGenerator root)
        {
            this.root = root;
        }

        public IntermediateFileDefinition WithUsing(string @namespace)
        {
            Usings.Add(@namespace);
            return this;
        }

        public IntermediateNamespaceDefintion Namespace(string @namespace)
        {
            var definition = Namespaces.GetOrAdd(@namespace, () => new(root, @namespace));

            return definition;
        }
        public void Generate(CodeBuilder builder)
        {
            foreach (var usings in Usings.OrderByDescending(x => x, StringComparer.InvariantCultureIgnoreCase))
                builder.AppendLine($"using {usings};");

            if (Usings.Any())
                builder.NewLine();

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

            public IntermediateTypeDefinition Type(string name, MemberFlags flags)
            {
                flags = flags | MemberFlags.Type;

                var definition = Types.GetOrAdd(name, () => new IntermediateTypeDefinition(root, flags: flags, name: name));

                return definition;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .AppendLine($"namespace {Namespace};")
                    .NewLine()
                    .Append((b, i) => i.Value.Generate(b), Types);
            }
        }

        public abstract class IntermediateMemberDefinition
        {
            public string Name { get; set; }

            public MemberFlags Flags { get; set; }

            public IntermediateMemberDefinition(MemberFlags flags, string name)
            {
                Name = name;
                Flags = flags;
            }

            protected void AppendMemberFlags(CodeBuilder builder)
            {
                if (Flags.HasFlag(MemberFlags.Public))
                    builder.AppendWord("public");

                if (Flags.HasFlag(MemberFlags.Static))
                    builder.AppendWord("static");

                if (Flags.HasFlag(MemberFlags.Override))
                    builder.AppendWord("override");
            }

            protected void MemberName(CodeBuilder builder)
            {
                builder.AppendWord(Name);
            }
        }


        public abstract class IntermediateTypedMemberDefinition : IntermediateMemberDefinition
        {
            public CsharpTypeReference Type { get; set; }

            protected IntermediateTypedMemberDefinition(MemberFlags flags, CsharpTypeReference type, string name) : base(flags, name)
            {
                Type = type;
            }
        }

        public class IntermediatePropertyDefinition : IntermediateTypedMemberDefinition, IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public IntermediatePropertyDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name) 
            {
                this.root = root;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append(AppendMemberFlags)
                    .AppendWord(Type.Resolve(root))
                    .Append(MemberName)
                    .AppendLine("{ get; set; }");
            }


            public void GetRequiredReferences(HashSet<CsharpTypeReference> refs)
            {
                refs.Add(Type);
            }
        }

        public class IntermediateArgumentDefinition : IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public string Name { get; }

            public CsharpTypeReference Type { get; }

            public IntermediateArgumentDefinition(CSharpCodeGenerator root, CsharpTypeReference type, string name)
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

            public IntermediateMethodDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name)
            {
                Body = CodeBuilder.FromMemoryStream(out var stream);
                BodyStream = stream;
                this.root = root;
            }

            public IntermediateMethodDefinition WithArgument(CsharpTypeReference type, string name)
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
                    .Append(AppendMemberFlags).AppendWord(Type.Resolve(root)).Append(MemberName)
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

            public IntermediateTypeDefinition(CSharpCodeGenerator root, MemberFlags flags, string name) : base(flags, name)
            {
                this.root = root;
            }

            public IntermediatePropertyDefinition Property(MemberFlags flags, CsharpTypeReference type, string name)
            {
                var definition = Properties.GetOrAdd(name, () => new(root, flags, type, name));

                return definition;
            }

            public IntermediateMethodDefinition Method(MemberFlags flags, CsharpTypeReference returnType, string name)
            {
                var definition = Methods.GetOrAdd(name, () => new(root, flags, returnType, name));

                definition.Flags = definition.Flags | flags;

                // TODO throw exception if return types don't match

                return definition;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append(AppendMemberFlags)
                    .AppendWord("class")
                    .Append(MemberName)
                    .OpenBlock()
                        .Append((b, p) => p.Value.Generate(b), Properties);

                if (Properties.Any() && Methods.Any())
                    builder.NewLine();

                builder
                        .Append((b, p) => p.Value.Generate(b), Methods)
                    .CloseBlock();
            }
        }
    }
}
