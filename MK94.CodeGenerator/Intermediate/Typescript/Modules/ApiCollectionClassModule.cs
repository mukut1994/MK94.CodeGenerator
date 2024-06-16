using MK94.CodeGenerator.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class ApiCollectionClassModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly IFeatureGroup<TypescriptCodeGenerator> project;
    private readonly string FileName;

    private Func<TypeDefinition, string, string> GetPropertyName = (_, x) => x;

    public ApiCollectionClassModule(IFeatureGroup<TypescriptCodeGenerator> project, string fileName)
    {
        this.project = project;
        FileName = fileName;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        var file = codeGenerator.File($"{FileName}.ts");
        var type = file.Type("Api", MemberFlags.Public);

        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                var name = typeDef.GetTypeName();

                type.Property(MemberFlags.Public | MemberFlags.Static, TsTypeReference.ToAnonymous(), GetPropertyName(typeDef, name))
                    .WithDefaultValue(name);
            }
        }
    }

    public ApiCollectionClassModule WithPropertyName(Func<TypeDefinition, string, string> nameSetter)
    {
        GetPropertyName = nameSetter;
        return this;
    }
}

public static class ApiCollectionClassModuleExtensions
{
    public static T WithApiCollectionClassModuleGenerator<T>(this T project,
        string fileName, 
        Action<ApiCollectionClassModule>? configure = null)
        where T : IFeatureGroup<TypescriptCodeGenerator>
    {
        var mod = new ApiCollectionClassModule(project, fileName);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}