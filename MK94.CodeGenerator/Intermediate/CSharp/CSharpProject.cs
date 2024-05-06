using System;
using System.Collections.Generic;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;

namespace MK94.CodeGenerator.Intermediate.CSharp;

public interface ICSharpProject :
    IProject,
    IGeneratorModuleUser<CSharpCodeGenerator>
{
    List<FeatureGroup<CSharpCodeGenerator>> FeatureGroups { get; }
}

public class CSharpProject : Project<CSharpCodeGenerator>, ICSharpProject
{
    public string RelativePath { get; set; }
    public List<IGeneratorModule<CSharpCodeGenerator>> GeneratorModules { get; } = new();

    public CSharpProject(Solution solution, string relativePath) : base(solution)
    {
        RelativePath = relativePath;
    }

    public override void Generate(Func<string, CodeBuilder> outputFactory)
    {
        var output = new CSharpCodeGenerator();

        foreach(var rootGenerator in GeneratorModules)
            rootGenerator.AddTo(output);

        foreach (var group in FeatureGroups)
        {
            foreach(var generator in group.GeneratorModules)
                generator.AddTo(output);
        }

        output.Generate(path => outputFactory(System.IO.Path.Combine(RelativePath, path)));
    }
}
