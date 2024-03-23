using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class HttpClientModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public HttpClientModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                if (!typeDef.Methods.Any())
                    continue;

                var file = codeGenerator.File($"{fileDef.Name}.ts");

                var type = file.Type(typeDef.AsClassName() + "Api", MemberFlags.Public);

                foreach(var methodDef in typeDef.Methods)
                {
                    var method = type.Method(MemberFlags.Public | MemberFlags.Static | MemberFlags.Async,
                        TsTypeReference.ToPromiseType(methodDef.ResponseType),
                        methodDef.Name);

                    foreach (var argDef in methodDef.Parameters)
                        method.WithArgument(TsTypeReference.ToType(argDef.Type), argDef.Name);

                    method.WithArgument(TsTypeReference.ToAnonymous(), "f", "fetch");

                    method.Body.AppendLine($@"const ret = await f(""api/v1/{typeDef.AsClassName()}/{method.Name}"")");

                    // SetQueryParams(methodDef, method);

                    method.Body.Append($@"return ret.json();");
                }
            }
        }
    }
}

public static class HttpClientModuleModuleExtensions
{
    public static T WithHttpClientModuleGenerator<T>(this T project, Action<HttpClientModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new HttpClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}