﻿using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript;

public interface IGenerator
{
    void Generate(CodeBuilder builder);

    void GetRequiredReferences(HashSet<TsTypeReference> refs) { }
}

public record TypeResolveContext(TypescriptCodeGenerator Root, string FilePath, IntermediateFileDefinition File);

public record TypeResolveMatch(string? import, string? name, string? importName = null);

public abstract record TsTypeReference
{
    public static TsTypeReference ToRaw(string type)
    {
        return new StringTypedTypeReference(type);
    }

    public static TsTypeReference ToNamed(string? importFrom, string name)
    {
        return new NamedTypeReference(importFrom, name);
    }

    public static TsTypeReference ToAnonymous()
    {
        return new AnonymousReference();
    }

    public static TsTypeReference ToType<T>()
    {
        return new TypedTypeReference(typeof(T));
    }

    public static TsTypeReference ToType(Type t)
    {
        return new TypedTypeReference(t);
    }

    public static TsTypeReference ToPromiseType(Type t)
    {
        return new PromiseTypedTypeReference(t);
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

internal record PromiseTypedTypeReference : TsTypeReference
{
    private TsTypeReference helperReference;

    public PromiseTypedTypeReference(Type type)
    {
        helperReference = TsTypeReference.ToType(type.UnwrapTask());
    }

    public override TypeResolveMatch Resolve(TypeResolveContext context)
    {
        return new TypeResolveMatch(null, $"Promise<{helperReference.Resolve(context).name}>");
    }
}

internal record AnonymousReference : TsTypeReference
{
    public override TypeResolveMatch Resolve(TypeResolveContext context)
    {
        return new(null, null);
    }
}

internal record NamedTypeReference : TsTypeReference
{
    public string Name { get; private init; }
    public string? ImportFrom { get; private init; }

    public NamedTypeReference(string? importFrom, string name)
    {
        Name = name;
        ImportFrom = importFrom;
    }

    public override TypeResolveMatch Resolve(TypeResolveContext context)
    {
        return new(ImportFrom, Name);
    }
}

internal record TypedTypeReference : TsTypeReference
{
    private readonly Type type;
    
    public TypedTypeReference(Type type)
    {
        this.type = type;
    }

    public override TypeResolveMatch Resolve(TypeResolveContext context)
    {
        return Resolve(type, context);
    }

    private TypeResolveMatch Resolve(Type type, TypeResolveContext context)
    {
        if (context.Root.TypeNameLookups.TryGetValue(type, out var lookup))
            return new(context.Root.RelativeFileResolver.GetImportPath(context.File.FileName, type), lookup);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var ret = Resolve(type.GetGenericArguments()[0], context);

            return new(ret.import, ret.name + " | null", ret.name);
        }

        return new(context.Root.RelativeFileResolver.GetImportPath(context.File.FileName, type), type.Name);
    }
}

internal record StringTypedTypeReference : TsTypeReference
{
    private readonly string type;

    public StringTypedTypeReference(string type)
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

public class TypescriptCodeGenerator : IFileGenerator
{
    public Dictionary<Type, string> TypeNameLookups = new()
    {
        { typeof(bool), "boolean" },
        { typeof(byte), "number" },
        { typeof(sbyte), "number" },
        { typeof(char), "number" },
        { typeof(decimal), "number" },
        { typeof(double), "number" },
        { typeof(float), "number" },
        { typeof(int), "number" },
        { typeof(uint), "number" },
        { typeof(nint), "number" },
        { typeof(nuint), "number" },
        { typeof(long), "number" },
        { typeof(ulong), "number" },
        { typeof(short), "number" },
        { typeof(ushort), "number" },
        { typeof(string), "string" },
        { typeof(Guid), "string" },
        { typeof(Task), "void" }
    };

    public RelativeFileResolver RelativeFileResolver { get; }

    public TypescriptCodeGenerator(RelativeFileResolver resolver)
    {
        RelativeFileResolver = resolver;
    }

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
    public string FileName { get; }

    private TypeResolveContext context { get; }

    public Dictionary<string, IntermediateMemberDefinition> Members { get; } = new();

    public Dictionary<string, HashSet<string>> Imports { get; } = new();

    public IntermediateFileDefinition(TypescriptCodeGenerator root, string fileName)
    {
        context = new(root, fileName, this);

        FileName = fileName;
    }

    public IntermediateTypeDefinition Type(string name, MemberFlags flags)
    {
        flags = flags | MemberFlags.Type;

        // TODO validation
        var definition = (IntermediateTypeDefinition) Members
            .GetOrAdd(name, () => new IntermediateTypeDefinition(context, flags: flags, name: name));

        return definition;
    }

    public IntermediateEnumDefinition Enum(string name, MemberFlags flags)
    {
        var ret = new IntermediateEnumDefinition(name, flags);

        Members.Add(name, ret);

        return ret;
    }

    //public IntermediateFileDefinition WithImport(string path, string type)
    //{
    //    var set = Imports.GetOrAdd(path, () => new());
    //
    //    set.Add(type);
    //
    //    return this;
    //}

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
                .AppendWord("{")
                .Append((b, x) => b.AppendWord(x.importName ?? x.name!).AppendOptionalComma(), importFile)
                .EndOptionalComma()
                .AppendWord("}")
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

    public MemberFlags Flags { get; set; }

    public IntermediateMemberDefinition(MemberFlags flags, string name)
    {
        Name = name;
        Flags = flags;
    }

    public void WriteMemberFlags(CodeBuilder builder)
    {
        if (Flags.HasFlag(MemberFlags.Public) && Flags.HasFlag(MemberFlags.Type))
            builder.AppendWord("export");

        if (Flags.HasFlag(MemberFlags.Static))
            builder.AppendWord("static");

        if (Flags.HasFlag(MemberFlags.Async) && Flags.HasFlag(MemberFlags.Method))
            builder.AppendWord("async");
    }

    public void MemberName(CodeBuilder builder)
    {
        builder.Append(Name);
    }

    public abstract IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context);

    public abstract void Generate(CodeBuilder builder);
}

public abstract class IntermediateTypedMemberDefinition : IntermediateMemberDefinition
{
    public TsTypeReference Type { get; set; }

    protected IntermediateTypedMemberDefinition(MemberFlags flags, TsTypeReference type, string name) : base(flags, name)
    {
        Type = type;
    }
}

public class IntermediatePropertyDefinition : IntermediateTypedMemberDefinition, IGenerator
{
    private TypeResolveContext context { get; }

    public IntermediatePropertyDefinition(TypeResolveContext context, MemberFlags flags, TsTypeReference type, string name) : base(flags, type, name)
    {
        this.context = context;
    }

    public override void Generate(CodeBuilder builder)
    {
        var type = Type.Resolve(context).name;

        builder
            .Append(WriteMemberFlags)
            .Append(MemberName);

        if (type != null)
            builder
                .Append(": ")
                .Append(type);

        builder
            .Append(";")
            .NewLine();
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

    public string? Default { get; set; }

    public IntermediateArgumentDefinition(TypeResolveContext context, TsTypeReference type, string name, string? @default)
    {
        Name = name;
        Type = type;
        Default = @default;

        this.context = context;
    }

    public void Generate(CodeBuilder builder)
    {
        builder.Append(Name);

        var typeName = Type.Resolve(context).name;

        if (typeName != null)
            builder.AppendWord(":").AppendWord(typeName);

        if (Default != null)
            builder.AppendWord(" =").Append(Default);

        builder.AppendOptionalComma();
    }
}

public class IntermediateMethodDefinition : IntermediateTypedMemberDefinition, IGenerator
{
    private TypeResolveContext context { get; }
    public MemoryStream BodyStream { get; }
    public CodeBuilder Body { get; }
    public List<IntermediateArgumentDefinition> Arguments { get; } = new();

    public IntermediateMethodDefinition(TypeResolveContext context, MemberFlags flags, TsTypeReference type, string name) 
        : base(flags, type, name)
    {
        Flags |= MemberFlags.Method;

        Body = CodeBuilder.FromMemoryStream(out var stream);
        BodyStream = stream;
        this.context = context;
    }

    public IntermediateMethodDefinition WithArgument(TsTypeReference type, string name, string? @default = null)
    {
        Arguments.Add(new(context, type, name, @default));

        return this;
    }

    public override void Generate(CodeBuilder builder)
    {
        builder.NewLine();

        Body.Flush();
        BodyStream.Flush();
        BodyStream.Position = 0;

        var retTypeName = Type.Resolve(context).name;

        builder
            .Append(WriteMemberFlags).Append(MemberName)
            .OpenParanthesis()
                .Append((b, arg) => arg.Generate(b), Arguments)
            .CloseParanthesis();

        if (retTypeName != null)
            builder.Append(": ").Append(retTypeName);

        builder
            .WithBlock(b => b.Append(BodyStream));
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

    public IntermediateTypeDefinition(
        TypeResolveContext context, 
        MemberFlags flags,
        string name) : base(flags, name)
    {
        this.context = context;
    }

    public IntermediatePropertyDefinition Property(MemberFlags flags, TsTypeReference type, string name)
    {
        var definition = Properties.GetOrAdd(name, () => new(context, flags, type, name));

        return definition;
    }

    public IntermediateMethodDefinition Method(MemberFlags flags, TsTypeReference type, string name)
    {
        var definition = Methods.GetOrAdd(name, () => new(context, flags, type, name));

        return definition;
    }

    public override void Generate(CodeBuilder builder)
    {
        builder
            .Append(WriteMemberFlags)
            .AppendWord(Flags.HasFlag(MemberFlags.Interface) ? "interface" : "class")
            .Append(MemberName)
            .OpenBlock()
                .Append((b, p) => p.Value.Generate(b), Properties);

        if (Properties.Any() && Methods.Any())
            builder.NewLine();

        builder
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


public class IntermediateEnumDefinition : IntermediateMemberDefinition, IGenerator
{
    private List<(string key, string value)> keyValues = new();

    public IntermediateEnumDefinition(string name, MemberFlags flags) : base(flags | MemberFlags.Type, name)
    {
    }

    public IntermediateEnumDefinition WithKeyValue(string key, string value)
    {
        keyValues.Add((key, value));

        return this;
    }

    public override void Generate(CodeBuilder builder)
    {
        WriteMemberFlags(builder);

        builder
            .AppendWord("enum")
            .AppendWord(Name)
            .OpenBlock()
            .Append((b, kv) =>
            {
                b.Append($"{kv.key} = {kv.value},")
                .NewLine();
            }, keyValues)
            .CloseBlock()
            .NewLine();
    }

    public override IEnumerable<TypeResolveMatch> ResolveReferences(TypeResolveContext context)
    {
        yield break;
    }
}