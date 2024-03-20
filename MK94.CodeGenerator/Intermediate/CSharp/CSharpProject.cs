using System;
using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp;

public interface ICSharpProject : 
    IProject, 
    IGeneratorModuleUser<CSharpCodeGenerator>, 
    INamespaceResolver<CSharpCodeGenerator>,
    ICodeGenerator<CSharpCodeGenerator, ICSharpProject>
{ }

public class CSharpProject : Project, ICSharpProject
{
    public Func<TypeDefinition, string> NamespaceResolver { get; set; } = _ => "NotSet";

    public string RelativePath { get; set; }
    public List<IGeneratorModule<CSharpCodeGenerator>> GeneratorModules { get; } = new();

    public CSharpProject(Solution solution, string relativePath) : base(solution)
    {
        RelativePath = relativePath;
    }

    public ICSharpProject GenerateTo(CSharpCodeGenerator target)
    {
        foreach (var gen in GeneratorModules)
            gen.AddTo(target);

        return this;
    }
}
