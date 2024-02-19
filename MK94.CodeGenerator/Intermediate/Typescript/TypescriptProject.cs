using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript;

public interface ITypescriptProject : IProject
{
    string BasePath { get; set; }

    List<IGeneratorModule<TypescriptCodeGenerator>> GeneratorModules { get; }

    ITypescriptProject GenerateTo(TypescriptCodeGenerator target);
}

public class TypescriptProject : Project, ITypescriptProject
{
    public string BasePath { get; set; }

    public List<IGeneratorModule<TypescriptCodeGenerator>> GeneratorModules { get; } = new();

    public TypescriptProject(Solution solution, string basePath)
        : base(solution)
    {
        Solution = solution;
        BasePath = basePath;
    }

    public ITypescriptProject GenerateTo(TypescriptCodeGenerator target)
    {
        foreach (var gen in GeneratorModules)
            gen.AddTo(target);

        return this;
    }
}
