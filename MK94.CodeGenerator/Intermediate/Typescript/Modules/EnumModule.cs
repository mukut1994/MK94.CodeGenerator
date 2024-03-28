using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class EnumModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public EnumModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var enumDef in fileDef.EnumTypes)
            {
                var file = codeGenerator.File($"{fileDef.Name}.ts");

                var @enum = file.Enum(enumDef.Type.Name, MemberFlags.Public | MemberFlags.Interface);

                foreach(var kvPair in enumDef.KeyValuePairs)
                {
                    @enum.WithKeyValue(kvPair.Key, kvPair.Value.ToString());
                }
            }
        }
    }
}

public static class EnumModuleExtensions
{
    public static T WithEnumsGenerator<T>(this T project, Action<EnumModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new EnumModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}