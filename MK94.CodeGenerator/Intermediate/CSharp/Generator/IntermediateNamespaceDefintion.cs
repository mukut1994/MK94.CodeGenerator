using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateNamespaceDefintion : IGenerator
{
    private CSharpCodeGenerator root { get; }

    public string Namespace { get; }

    public Dictionary<string, IntermediateTypeDefinition> Types { get; } = new();
    public Dictionary<string, IntermediateEnumDefinition> Enums { get; } = new();

    public IntermediateNamespaceDefintion(CSharpCodeGenerator root, string @namespace)
    {
        Namespace = @namespace;
        this.root = root;
    }

    public IntermediateTypeDefinition Type(string name, MemberFlags flags, CsharpTypeReference type)
    {
        flags = flags | MemberFlags.Type;

        var definition = Types.GetOrAdd(name, () => new IntermediateTypeDefinition(root, flags: flags, name: name, type: type));

        return definition;
    }
    public IntermediateEnumDefinition Enum(string name, MemberFlags flags)
    {
        flags = flags | MemberFlags.Type;

        var definition = Enums.GetOrAdd(name, () => new IntermediateEnumDefinition(name: name, flags: flags));

        return definition;
    }

    public void Generate(CodeBuilder builder)
    {
        builder
            .AppendLine($"namespace {Namespace};")
            .NewLine()
            .Append((b, i) => i.Value.Generate(b), Enums)
            .Append((b, i) => i.Value.Generate(b), Types);
    }
}
