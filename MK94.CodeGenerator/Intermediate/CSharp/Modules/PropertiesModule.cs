using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class PropertiesModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public PropertiesModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

                var ns = file.Namespace(typeDef.GetNamespace());
                var type = ns.Type(typeDef.Type.Name, MemberFlags.Public, CsharpTypeReference.ToType(typeDef.Type));

                foreach(var propertyDef in typeDef.Properties)
                {
                    type.Property(
                        MemberFlags.Public, 
                        CsharpTypeReference.ToType(propertyDef.Type),
                        propertyDef.Name);
                }
            }
        }
    }
}

public static class DataModuleExtensions
{
    public static T WithPropertiesGenerator<T>(this T project, Action<PropertiesModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new PropertiesModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}