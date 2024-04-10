using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class StronglyTypedIdsModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public StronglyTypedIdsModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                codeGenerator.File($"{fileDef.Name}.ts")
                    .WithTypeAlias(TsTypeReference.CleanName(typeDef.Type), MemberFlags.Public | MemberFlags.Interface, "string");
            }
        }
    }
}

public static class StronglyTypedIdsModuleExtensions
{
    public static T WithStronglyTypedIdsGenerator<T>(this T project, Action<StronglyTypedIdsModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new StronglyTypedIdsModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}