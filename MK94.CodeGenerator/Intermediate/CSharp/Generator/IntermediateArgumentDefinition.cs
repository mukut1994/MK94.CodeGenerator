using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

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
