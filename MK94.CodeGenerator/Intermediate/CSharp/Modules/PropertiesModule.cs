using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class PropertiesModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public PropertiesModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                if (!typeDef.Properties.Any())
                    continue;

                var file = codeGenerator.File($"{fileDef.Name}.g.cs");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));
                var type = ns.Type(typeDef.Type.Name, MemberFlags.Public, DefinitionType.Class);

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
        where T : ICSharpProject
    {
        var mod = new PropertiesModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}