using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript;

public interface ITypescriptProject : IProject
{
    string RelativePath { get; set; }

    List<IGeneratorModule<TypescriptCodeGenerator>> GeneratorModules { get; }

    ITypescriptProject GenerateTo(TypescriptCodeGenerator target);

    List<FeatureGroup<TypescriptCodeGenerator>> FeatureGroups { get; }
}

public class TypescriptProject : Project<TypescriptCodeGenerator>, ITypescriptProject
{
    public string RelativePath { get; set; }

    public List<IGeneratorModule<TypescriptCodeGenerator>> GeneratorModules { get; } = new();

    public TypescriptProject(Solution solution, string relativePath)
        : base(solution)
    {
        Solution = solution;
        RelativePath = relativePath;
    }

    public ITypescriptProject GenerateTo(TypescriptCodeGenerator target)
    {
        foreach (var gen in GeneratorModules)
            gen.AddTo(target);

        return this;
    }

    public override void Generate(Func<string, CodeBuilder> outputFactory)
    {
        var files = FeatureGroups.SelectMany(x => x.Files).ToList();

        var output = new TypescriptCodeGenerator(new(files));

        foreach(var l in Solution.TypescriptTypeLookups)
            output.TypeNameLookups[l.Key] = l.Value;

        foreach (var group in FeatureGroups)
        {
            // TODO hacky fix because relative file resolver is added to ts code gen when it should be created at this point
            output.RelativeFileResolver.files = group.Files;

            foreach (var generator in group.GeneratorModules)
                generator.AddTo(output);
        }

        output.Generate(path => outputFactory(System.IO.Path.Combine(RelativePath, path)));
    }
}
