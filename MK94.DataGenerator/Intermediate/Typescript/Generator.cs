using MK94.CodeGenerator.Attributes;
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

namespace MK94.CodeGenerator.Intermediate.Typescript
{
    public interface IGenerator
    {
        void Generate(CodeBuilder builder);

        void GetRequiredReferences(HashSet<TsTypeReference> refs) { }
    }

    public record TypeResolveContext(TypescriptCodeGenerator Root, string FilePath, IntermediateFileDefinition File);

    public record TypeResolveMatch(string? import, string name);

    public abstract record TsTypeReference
    {
        public static TsTypeReference ToRaw(string type)
        {
            return new TypedTypeReference(type);
        }

        public static TsTypeReference ToType<T>()
        {
            return new NamedTypeReference(typeof(T).FullName); // TODO
        }

        public abstract TypeResolveMatch Resolve(TypeResolveContext context);

        public enum TypeText
        {
            Generic,
            Import,
            Extension
        }

        public static string CleanName(Type type, TypeText mode = TypeText.Generic)
        {
            if (type == typeof(bool))
                return "boolean";
            else if (type == typeof(int))
                return "number";
            else if (type == typeof(decimal))
                return "number";
            else if (type == typeof(byte))
                return "number";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(Guid))
                return "string";
            else if (type == typeof(TimeSpan))
                return "string";
            else if (type == typeof(DateTime))
                return "Date";
            else if (type.Name == "IFileData")
                return "File";
            else if (type.Name == "IFormFile")
                return "File";
            else if (type == typeof(void) || type == typeof(Task))
                return "unknown";
            else if (type == typeof(Stream))
                return "Blob";
            else if (type == typeof(System.Numerics.Vector3))
                return "unknown";
            else if (type == typeof(System.Numerics.Quaternion))
                return "Quaternion";
            else if (type == typeof(byte[]))
                return "ArrayBuffer";
            else if (type == typeof(IFileResult))
                return "ArrayBuffer";

            else if (type.IsArray)
                return CleanName(type.GetElementType()!) + "[]";

            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Stack<>))
                    return CleanName(type.GetGenericArguments()[0]) + "[]";

                if (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return CleanName(type.GetGenericArguments()[0], mode);

                if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                    return $"{CleanName(type.GetGenericArguments()[0], mode)}[]";

                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return $"{{ [key: string]: {CleanName(type.GenericTypeArguments[1])} | null }}";

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var typeText = CleanName(type.GetGenericArguments()[0], mode);

                    if (mode == TypeText.Generic)
                        typeText += "[]";
                    else if (mode == TypeText.Extension)
                        typeText = $"Array<{typeText}>";

                    return typeText;
                }

                if (mode == TypeText.Import)
                    return type.Name.Remove(type.Name.IndexOf('`'));

                return type.Name.Remove(type.Name.IndexOf('`'))
                + "<"
                + type.GetGenericArguments().Select(x => CleanName(x, mode)).Aggregate((x, y) => $"{x}, {y}")
                + ">";
            }

            return type.Name;
        }
    }

    internal record NamedTypeReference : TsTypeReference
    {
        public string Name { get; private init; }

        public NamedTypeReference(string name)
        {
            Name = name;
        }

        public override TypeResolveMatch Resolve(TypeResolveContext context)
        {
            return new(null, Name);
        }
    }

    internal record TypedTypeReference : TsTypeReference
    {
        private readonly string type;

        public TypedTypeReference(string type)
        {
            this.type = type;
        }

        public override TypeResolveMatch Resolve(TypeResolveContext context)
        {
            var match = context.Root.Files
                .SelectMany(x => x.Value.Members.Select(m => new { file = x, member = m }))
                .Where(m => m.member.Key == type)
                .Single(); // TODO error message

            var directoryMatch = Path.GetDirectoryName(match.file.Key)!;
            var directoryCurrent = Path.GetDirectoryName(context.FilePath)!;

            string importPath;

            if (directoryMatch == directoryCurrent)
                importPath = match.file.Key;
            else 
                importPath = Path.Combine(Path.GetRelativePath(directoryCurrent, directoryMatch), Path.GetFileName(match.file.Key));

            return new (importPath, match.member.Key);
        }
    }

    public class TypescriptCodeGenerator
    {
        public Dictionary<string, IntermediateFileDefinition> Files { get; } = new();

        public IntermediateFileDefinition File(string fileName)
        {
            var definition = Files.GetOrAdd(fileName, () => new(this, fileName));

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
        private TypeResolveContext context { get; }

        public Dictionary<string, IntermediateMemberDefinition> Members { get; } = new();

        public IntermediateFileDefinition(TypescriptCodeGenerator root, string fileName)
        {
            this.context = new(root, fileName, this);
        }

        public IntermediateTypeDefinition Type(string name, BindingFlags flags)
        {
            // TODO validation
            var definition = (IntermediateTypeDefinition) Members.GetOrAdd(name, () => new IntermediateTypeDefinition(context, flags: flags, name: name));

            return definition;
        }

        public void Generate(CodeBuilder builder)
        {
            GenerateImports(builder);

            foreach (var member in Members)
            {
                member.Value.Generate(builder);
            }
        }

        private void GenerateImports(CodeBuilder builder)
        {
            var anyImports = false;
            var imports = Members.SelectMany(m => m.Value.ResolveReferences(context))
                .Distinct()
                .Where(x => x.import != null)
                .GroupBy(x => x.import);

            foreach(var importFile in imports)
            {
                builder.AppendWord("import")
                    .OpenBlock()
                        .Append((b, x) => b.AppendWord(x.name).AppendOptionalComma(), importFile)
                    .CloseBlock()
                    .AppendWord("from")
                    .AppendWord(@$"""{importFile.Key}"";")
                    .NewLine();

                anyImports = true;
            }

            if (anyImports)
                builder.NewLine();
        }
    }

    public abstract class IntermediateMemberDefinition : IGenerator
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

        public abstract IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context);

        public abstract void Generate(CodeBuilder builder);
    }

    public abstract class IntermediateTypedMemberDefinition : IntermediateMemberDefinition
    {
        public TsTypeReference Type { get; set; }

        protected IntermediateTypedMemberDefinition(BindingFlags flags, TsTypeReference type, string name) : base(flags, name)
        {
            Type = type;
        }
    }

    public class IntermediatePropertyDefinition : IntermediateTypedMemberDefinition, IGenerator
    {
        private TypeResolveContext context { get; }

        public IntermediatePropertyDefinition(TypeResolveContext context, BindingFlags flags, TsTypeReference type, string name) : base(flags, type, name)
        {
            this.context = context;
        }

        public override void Generate(CodeBuilder builder)
        {
            builder
                .Append(MemberFlags)
                .AppendWord(Type.Resolve(context).name)
                .Append(MemberName)
                .AppendLine("{ get; set; }");
        }


        public void GetRequiredReferences(HashSet<TsTypeReference> refs)
        {
            refs.Add(Type);
        }

        public override IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context)
        {
            yield return Type.Resolve(context);
        }
    }

    public class IntermediateArgumentDefinition : IGenerator
    {
        private TypeResolveContext context { get; }

        public string Name { get; }

        public TsTypeReference Type { get; }

        public IntermediateArgumentDefinition(TypeResolveContext context, TsTypeReference type, string name)
        {
            Name = name;
            Type = type;
            this.context = context;
        }

        public void Generate(CodeBuilder builder)
        {
            builder.AppendWord(Type.Resolve(context).name).AppendWord(Name).AppendOptionalComma();
        }
    }

    public class IntermediateMethodDefinition : IntermediateTypedMemberDefinition, IGenerator
    {
        private TypeResolveContext context { get; }
        public MemoryStream BodyStream { get; }
        public CodeBuilder Body { get; }
        public List<IntermediateArgumentDefinition> Arguments { get; } = new();

        public IntermediateMethodDefinition(TypeResolveContext context, BindingFlags flags, TsTypeReference type, string name) : base(flags, type, name)
        {
            Body = CodeBuilder.FromMemoryStream(out var stream);
            BodyStream = stream;
            this.context = context;
        }

        public IntermediateMethodDefinition WithArgument(TsTypeReference type, string name)
        {
            Arguments.Add(new(context, type, name));

            return this;
        }

        public override void Generate(CodeBuilder builder)
        {
            Body.Flush();
            BodyStream.Flush();
            BodyStream.Position = 0;

            builder
                .Append(MemberFlags).AppendWord(Type.Resolve(context).name).Append(MemberName)
                .OpenParanthesis()
                    .Append((b, arg) => arg.Generate(b), Arguments)
                .CloseParanthesis()
                .WithBlock(b => b.Append(BodyStream))
                ;
        }

        public override IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context)
        {
            yield return Type.Resolve(context);

            foreach (var arg in Arguments)
                yield return arg.Type.Resolve(context);
        }
    }

    public class IntermediateTypeDefinition : IntermediateMemberDefinition, IGenerator
    {
        private TypeResolveContext context { get; }
        public Dictionary<string, IntermediatePropertyDefinition> Properties = new();
        public Dictionary<string, IntermediateMethodDefinition> Methods = new();

        public IntermediateTypeDefinition(TypeResolveContext context, BindingFlags flags, string name) : base(flags, name)
        {
            this.context = context;
        }

        public IntermediatePropertyDefinition Property(BindingFlags flags, TsTypeReference type, string name)
        {
            var definition = Properties.GetOrAdd(name, () => new(context, flags, type, name));

            return definition;
        }

        public IntermediateMethodDefinition Method(BindingFlags flags, TsTypeReference type, string name)
        {
            var definition = Methods.GetOrAdd(name, () => new(context, flags, type, name));

            return definition;
        }

        public override void Generate(CodeBuilder builder)
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
        public override IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context)
        {
            foreach (var props in Properties)
                yield return props.Value.Type.Resolve(context);

            foreach (var methods in Methods)
                foreach (var resolved in methods.Value.ResolveReferences(context))
                    yield return resolved;
        }
    }
}
