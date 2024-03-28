using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class FetchClientModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public ControllerResolver ControllerResolver { get; private set; } = ControllerResolver.Instance;

    public FetchClientModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public FetchClientModule WithResolver(ControllerResolver resolver)
    {
        ControllerResolver = resolver;
        return this;
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

                    method.WithArgument(TsTypeReference.ToAnonymous(), "f", "fetch");
                    method.WithArgument(TsTypeReference.ToNamed(null, "RequestInit"), "init");

                    var rewriteInit = false;
                    ParameterDefinition? bodyParam = null; 

                    foreach (var argDef in methodDef.Parameters)
                    {
                        method.WithArgument(TsTypeReference.ToType(argDef.Type), argDef.Name);

                        var isBodyParam = ControllerResolver.IsBodyParameter(argDef);

                        rewriteInit = isBodyParam || rewriteInit;

                        if (isBodyParam)
                            bodyParam = argDef;
                    }

                    if(rewriteInit)
                    {
                        method.Body.Append("init = {")
                                .IncreaseIndent()
                                    .NewLine()
                                    .AppendLine("...init,");

                        if (bodyParam != null)
                            method.Body.AppendLine($"body: JSON.stringify({bodyParam.Name}),");


                        method.Body
                                .DecreaseIndent()
                                .AppendLine("};")
                                .NewLine();
                    }

                    method.Body.AppendLine($@"const ret = await f(""{typeDef.AsClassName()}/{method.Name}"", init)");

                    // SetQueryParams(methodDef, method);

                    method.Body.Append($@"return ret.json();");
                }
            }
        }
    }
}

public static class FetchClientModuleModuleExtensions
{
    public static T WithFetchClientModuleGenerator<T>(this T project, Action<FetchClientModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new FetchClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}