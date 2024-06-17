using System;
using System.Collections.Generic;
using System.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateFileDefinition : IGenerator
{
    private CSharpCodeGenerator root { get; }

    public Dictionary<string, IntermediateNamespaceDefintion> Namespaces { get; } = new();
    public HashSet<string> ExternAliases {  get; } = new();

    public HashSet<string> Usings { get; } = new();

    public IntermediateFileDefinition(CSharpCodeGenerator root)
    {
        this.root = root;
    }

    public IntermediateFileDefinition WithExternalAlias(string alias)
    {
        ExternAliases.Add(alias);
        return this;
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
        foreach (var alias in ExternAliases.OrderByDescending(x => x, StringComparer.InvariantCultureIgnoreCase))
            builder.AppendLine($"extern alias {alias};");

        foreach (var usings in Usings.OrderByDescending(x => x, StringComparer.InvariantCultureIgnoreCase))
            builder.AppendLine($"using {usings};");

        if (Usings.Any())
            builder.NewLine();

        foreach (var @namespace in Namespaces)
        {
            @namespace.Value.Generate(builder);
        }
    }
}
