using System;
using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

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
