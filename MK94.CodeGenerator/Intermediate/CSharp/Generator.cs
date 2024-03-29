﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

                if (Flags.HasFlag(MemberFlags.Partial))
                    builder.AppendWord("partial");

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

            private List<IntermediateAttributeDefinition> attributes { get; } = new();

            public PropertyType PropertyType { get; set; }

            public string NewExpression { get; set; } = string.Empty;

            public IntermediatePropertyDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name) 
            {
                PropertyType |= PropertyType.Default;

                this.root = root;
            }

            public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
            {
                var ret = new IntermediateAttributeDefinition(root, attribute);
                
                attributes.Add(ret);

                return ret;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append((b, a) =>
                    {
                        a.Generate(b);
                        b.NewLine();
                    }, attributes)
                    .Append(AppendMemberFlags)
                    .AppendWord(Type.Resolve(root))
                    .Append(MemberName)
                    .Append(AppendPropertyType)
                    .Append(NewExpression)
                    .AppendLine(string.Empty);
            }

            public void GetRequiredReferences(HashSet<CsharpTypeReference> refs)
            {
                refs.Add(Type);
            }

            public void AppendPropertyType(CodeBuilder builder)
            {
                builder.AppendWord("{");

                if (PropertyType == 0 || PropertyType.HasFlag(PropertyType.Getter))
                    builder.AppendWord("get;");

                if (PropertyType == 0 || PropertyType.HasFlag(PropertyType.Setter))
                    builder.AppendWord("set;");

                if (PropertyType.HasFlag(PropertyType.Init))
                    builder.AppendWord("init;");

                builder.AppendWord("}");
            }

            public IntermediatePropertyDefinition WithGetter()
            {
                PropertyType |= PropertyType.Getter;

                return this;
            }

            public IntermediatePropertyDefinition WithSetter()
            {
                if (PropertyType.HasFlag(PropertyType.Init))
                    throw new InvalidOperationException("Property has `init` set up already.");

                PropertyType |= PropertyType.Setter;

                return this;
            }

            public IntermediatePropertyDefinition WithInit()
            {
                if (PropertyType.HasFlag(PropertyType.Setter))
                    throw new InvalidOperationException("Property has `set` set up already.");

                PropertyType |= PropertyType.Init;

                return this;
            }

            public IntermediatePropertyDefinition WithDefaultNew() => WithDefaultExpression("= new();");

            public IntermediatePropertyDefinition WithDefaultExpression(string newExpression)
            {
                NewExpression = newExpression;

                return this;
            } 
        }

        public class IntermediateAttributeDefinition : IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public CsharpTypeReference type { get; }

            private List<string> parameters { get; } = new();

            public IntermediateAttributeDefinition(CSharpCodeGenerator root, CsharpTypeReference type)
            {
                this.type = type;
                this.root = root;
            }

            public IntermediateAttributeDefinition WithParam(string parameter)
            {
                parameters.Add(parameter);

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                var attributeName = type.Resolve(root);

                if (attributeName.EndsWith("Attribute"))
                    attributeName = attributeName[..^"Attribute".Length];

                builder
                    .OpenSquareParanthesis()
                    .Append(attributeName)
                    .Append(AppendParameters)
                    .CloseSquareParanthesis();
            }

            public void GetRequiredReferences(HashSet<CsharpTypeReference> refs)
            {
                refs.Add(type);
            }

            private void AppendParameters(CodeBuilder builder)
            {
                if (parameters.Count == 0)
                    return;

                builder.OpenParanthesis();

                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i == parameters.Count - 1)
                    {
                        builder.Append(parameters[i]);
                    }
                    else
                    {
                        builder
                            .Append(parameters[i])
                            .AppendComma();
                    }
                }

                builder.CloseParanthesis();
            }
        }

        public class IntermediateArgumentDefinition : IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public string Name { get; }

            public CsharpTypeReference Type { get; }

            private List<IntermediateAttributeDefinition> attributes { get; } = new();

            private string? defaultValue { get; set; }

            public IntermediateArgumentDefinition(CSharpCodeGenerator root, CsharpTypeReference type, string name)
            {
                Name = name;
                Type = type;
                this.root = root;
            }

            public IntermediateArgumentDefinition DefaultValue(string defaultValue)
            {
                this.defaultValue = defaultValue;

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append((b, a) => a.Generate(b), attributes)
                    .AppendWord(Type.Resolve(root))
                    .AppendWord(Name)
                    .Append(AppendDefaultValue)
                    .AppendOptionalComma();
            }

            public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
            {
                var ret = new IntermediateAttributeDefinition(root, attribute);

                attributes.Add(ret);

                return ret;
            }

            private void AppendDefaultValue(CodeBuilder builder)
            {
                if (defaultValue is null)
                    return;

                builder.AppendWord($"= {defaultValue}");
            }
        }

        public class IntermediateMethodDefinition : IntermediateTypedMemberDefinition, IGenerator
        {
            public MemoryStream BodyStream { get; }

            public CodeBuilder Body { get; }

            public List<IntermediateArgumentDefinition> Arguments { get; } = new();

            private CSharpCodeGenerator root { get; }
            
            private List<IntermediateAttributeDefinition> attributes { get; } = new();

            public IntermediateMethodDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name)
            {
                Body = CodeBuilder.FromMemoryStream(out var stream);
                BodyStream = stream;
                this.root = root;
            }

            public IntermediateArgumentDefinition Argument(CsharpTypeReference type, string name)
            {
                var argument = new IntermediateArgumentDefinition(root, type, name);

                Arguments.Add(argument);

                return argument;
            }

            public IntermediateMethodDefinition WithArgument(CsharpTypeReference type, string name)
            {
                Argument(type, name);

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                Body.Flush();
                BodyStream.Flush();
                BodyStream.Position = 0;

                builder
                    .Append((b, a) =>
                    {
                        a.Generate(b);
                        b.NewLine();
                    }, attributes)
                    .Append(AppendMemberFlags)
                    .AppendWord(Type.Resolve(root))
                    .Append(MemberName)
                    .OpenParanthesis()
                        .Append((b, arg) => arg.Generate(b), Arguments)
                    .CloseParanthesis();

                if (Flags.HasFlag(MemberFlags.Partial) && BodyStream.Capacity == 0)
                {
                    builder.Append(";");
                    return;
                }
                
                builder.WithBlock(b => b.Append(BodyStream));
            }

            public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
            {
                var ret = new IntermediateAttributeDefinition(root, attribute);

                attributes.Add(ret);

                return ret;
            }
        }

        public class IntermediateConstructorDefinition : IntermediateMemberDefinition, IGenerator
        {
            public MemoryStream BodyStream { get; }

            public CodeBuilder Body { get; }

            public List<IntermediateArgumentDefinition> Arguments { get; } = new();

            private CSharpCodeGenerator root { get; }

            private string? baseConstructorCall { get; set; }

            public IntermediateConstructorDefinition(CSharpCodeGenerator root, MemberFlags flags, string name) : base(flags, name)
            {
                Body = CodeBuilder.FromMemoryStream(out var stream);
                BodyStream = stream;
                this.root = root;
            }

            public IntermediateArgumentDefinition WithArgument(CsharpTypeReference type, string name)
            {
                var argument = new IntermediateArgumentDefinition(root, type, name);

                Arguments.Add(argument);

                return argument;
            }

            public IntermediateConstructorDefinition WithBaseConstructorCall(string baseConstructorCall)
            {
                this.baseConstructorCall = baseConstructorCall;

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                Body.Flush();
                BodyStream.Flush();
                BodyStream.Position = 0;

                builder
                    .Append(AppendMemberFlags)
                    .Append(MemberName)
                    .OpenParanthesis()
                        .Append((b, arg) => arg.Generate(b), Arguments)
                    .CloseParanthesis()
                    .Append(AppendBaseConstructorCall)
                    .WithBlock(b => b.Append(BodyStream));
            }

            private void AppendBaseConstructorCall(CodeBuilder builder)
            {
                if (baseConstructorCall is null)
                    return;

                builder.AppendWord($": base({baseConstructorCall})");
            }
        }

        public class IntermediateTypeDefinition : IntermediateMemberDefinition, IGenerator
        {
            private CSharpCodeGenerator root { get; }

            public Dictionary<string, IntermediateTypeDefinition> Types = new();

            public Dictionary<string, IntermediatePropertyDefinition> Properties = new();

            public Dictionary<string, IntermediateMethodDefinition> Methods = new();
            private List<IntermediateAttributeDefinition> attributes { get; } = new();

            private List<IntermediateConstructorDefinition> constructors { get; } = new();

            private List<CsharpTypeReference> InheritsFrom { get; } = new();
            
            private DefinitionType DefinitionType { get; set; }

            private bool PrimaryConstructor { get; set; }

            public IntermediateTypeDefinition(CSharpCodeGenerator root, MemberFlags flags, string name) : base(flags, name)
            {
                this.root = root;
                DefinitionType |= DefinitionType.Default;
            }

            public IntermediateConstructorDefinition Constructor(MemberFlags flags)
            {
                var definition = new IntermediateConstructorDefinition(root, flags, Name);

                constructors.Add(definition);

                return definition;
            }

            public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
            {
                var ret = new IntermediateAttributeDefinition(root, attribute);

                attributes.Add(ret);

                return ret;
            }

            public IntermediatePropertyDefinition Property(MemberFlags flags, CsharpTypeReference type, string name)
            {
                var definition = Properties.GetOrAdd(name, () => new(root, flags, type, name));

                return definition;
            }

            public IntermediateMethodDefinition Method(MemberFlags flags, CsharpTypeReference returnType, string name)
            {
                var definition = Methods.GetOrAdd(name, () => new(root, flags, returnType, name));

                definition.Flags |= flags;

                // TODO throw exception if return types don't match

                return definition;
            }

            public IntermediateTypeDefinition Type(string name, MemberFlags flags)
            {
                flags |= MemberFlags.Type;

                var definition = Types.GetOrAdd(name, () => new IntermediateTypeDefinition(root, flags: flags, name: name));

                return definition;
            }

            public IntermediateTypeDefinition WithTypeAsClass()
            {
                if (DefinitionType != DefinitionType.Default)
                    throw new InvalidOperationException("The type has already been defined.");

                DefinitionType |= DefinitionType.Class;

                return this;
            }

            public IntermediateTypeDefinition WithTypeAsRecord()
            {
                if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Interface))
                    throw new InvalidOperationException("The type has already been defined.");

                DefinitionType |= DefinitionType.Record;

                return this;
            }

            public IntermediateTypeDefinition WithTypeAsStruct()
            {
                if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Interface))
                    throw new InvalidOperationException("The type has already been defined.");

                DefinitionType |= DefinitionType.Struct;

                return this;
            }

            public IntermediateTypeDefinition WithTypeAsInterface()
            {
                if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Struct) || DefinitionType.HasFlag(DefinitionType.Record))
                    throw new InvalidOperationException("The type cannot be defined as an interface.");

                DefinitionType = DefinitionType.Interface;

                return this;
            }

            public IntermediateTypeDefinition WithInheritsFrom(CsharpTypeReference inheritsFrom)
            {
                InheritsFrom.Add(inheritsFrom);

                return this;
            }

            public IntermediateTypeDefinition WithPrimaryConstructor()
            {
                PrimaryConstructor = true;

                return this;
            }

            public void Generate(CodeBuilder builder)
            {
                builder
                    .Append((b, a) =>
                    {
                        a.Generate(b);
                        b.NewLine();
                    }, attributes)
                    .Append(AppendMemberFlags)
                    .Append(AppendDefinitionFlags)
                    .Append(MemberName)
                    .Append(AppendPrimaryConstructor)
                    .Append(AppendInheritsFrom)
                    .OpenBlock();

                if (constructors.Any())
                {
                    builder.Append((b, p) => p.Generate(b), constructors);
                }

                if (Types.Any())
                {
                    if (constructors.Any())
                        builder.NewLine();

                    builder.Append((b, p) => p.Value.Generate(b), Types);
                }

                if (!PrimaryConstructor && Properties.Any())
                {
                    if (constructors.Any() || Types.Any())
                        builder.NewLine();

                    builder.Append((b, p) => p.Value.Generate(b), Properties);
                }

                if (Methods.Any())
                {
                    if (constructors.Any() || Types.Any() || Properties.Any())
                        builder.NewLine();

                    builder.Append((b, p) => p.Value.Generate(b), Methods);
                }

                builder.CloseBlock();
            }

            private void AppendDefinitionFlags(CodeBuilder builder)
            {
                if (DefinitionType == DefinitionType.Default || DefinitionType.HasFlag(DefinitionType.Class))
                    builder.AppendWord("class");

                if (DefinitionType.HasFlag(DefinitionType.Record))
                    builder.AppendWord("record");

                if (DefinitionType.HasFlag(DefinitionType.Struct))
                    builder.AppendWord("struct");

                if (DefinitionType.HasFlag(DefinitionType.Interface))
                    builder.AppendWord("interface");
            }

            private void AppendInheritsFrom(CodeBuilder builder)
            {
                if (InheritsFrom.Count == 0)
                    return;

                builder.AppendWord(":");

                for (var i = 0; i < InheritsFrom.Count; i++)
                {
                    if (i == InheritsFrom.Count - 1)
                    {
                        builder.AppendWord(InheritsFrom[i].Resolve(root));
                    }
                    else
                    {
                        builder.AppendWord($"{InheritsFrom[i]}, ");
                    }
                }
            }

            private void AppendPrimaryConstructor(CodeBuilder builder)
            {
                if (!PrimaryConstructor)
                    return;

                builder.OpenParanthesis();

                foreach (var property in Properties)
                {
                    if (property.Value.Flags.HasFlag(MemberFlags.Public))
                    {
                        builder.AppendWord(property.Value.Type.Resolve(root));
                        builder.AppendWord(property.Key);
                    }
                }

                builder.CloseParanthesis();

            }
        }
    }
}
