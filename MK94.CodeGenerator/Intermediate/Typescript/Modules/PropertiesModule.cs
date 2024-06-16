using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class PropertiesModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly IFeatureGroup<TypescriptCodeGenerator> project;

    public bool LowercaseFirst = true;

    public PropertiesModule(IFeatureGroup<TypescriptCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                var file = codeGenerator.File($"{fileDef.Name}.ts");

                var type = file.Type(TsTypeReference.CleanName(typeDef.Type), MemberFlags.Public | MemberFlags.Interface);

                if (typeDef.Type.BaseType != null && typeDef.Type.BaseType != typeof(object))
                    type.WithExtends(TsTypeReference.ToType(typeDef.Type.BaseType));

                foreach(var propertyDef in typeDef.Properties)
                {
                    var name = propertyDef.Name;

                    if (LowercaseFirst)
                        name = name.ToCamelCase();

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
        where T : IFeatureGroup<TypescriptCodeGenerator>
    {
        var mod = new PropertiesModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}