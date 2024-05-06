using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

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
