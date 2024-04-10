using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class ApiCollectionClassModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;
    private readonly string FileName;

    public ApiCollectionClassModule(ITypescriptProject project, string fileName)
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
                var name = typeDef.AsApiName();

                type.Property(MemberFlags.Public | MemberFlags.Static, TsTypeReference.ToAnonymous(), name)
                    .WithDefaultValue(typeDef.AsClassName() + "Api");
            }
        }
    }
}

public static class ApiCollectionClassModuleExtensions
{
    public static T WithApiCollectionClassModuleGenerator<T>(this T project,
        string fileName, 
        Action<ApiCollectionClassModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new ApiCollectionClassModule(project, fileName);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}