using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class PropertiesModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public bool LowercaseFirst = true;

    public PropertiesModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                if (!typeDef.Properties.Any())
                    continue;

                var file = codeGenerator.File($"{fileDef.Name}.ts");

                var type = file.Type(typeDef.Type.Name, MemberFlags.Public | MemberFlags.Interface);

                foreach(var propertyDef in typeDef.Properties)
                {
                    var name = propertyDef.Name;

                    if (LowercaseFirst)
                        name = name.ToLowercaseFirst();

                    type.Property(
                        MemberFlags.Public, 
                        TsTypeReference.ToType(propertyDef.Type),
                        name);
                }
            }
        }
    }

    public PropertiesModule WithUnchangedPropertyNames()
    {
        LowercaseFirst = false;
        return this;
    }
}

public static class PropertiesModuleExtensions
{
    public static T WithPropertiesGenerator<T>(this T project, Action<PropertiesModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new PropertiesModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}