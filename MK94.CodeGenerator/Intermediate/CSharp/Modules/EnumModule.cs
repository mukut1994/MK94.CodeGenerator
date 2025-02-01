using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using MK94.CodeGenerator.Intermediate.Typescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;


public class EnumModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public EnumModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            foreach (var enumDef in fileDef.EnumTypes)
            {
                var file = codeGenerator.File($"{fileDef.GetFilename()}.cs");
                var ns = file.Namespace(enumDef.GetNamespace());

                var @enum = ns.Enum(enumDef.Type.Name, MemberFlags.Public | MemberFlags.Interface);

                foreach (var kvPair in enumDef.KeyValuePairs)
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
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new EnumModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}