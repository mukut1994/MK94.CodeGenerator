using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Generator;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class ControllerClientModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public ControllerClientModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            foreach (var typeDef in fileDef.Types)
            {
                if (typeDef.Methods.Count == 0)
                    continue;

                if (typeDef.Type.GetCustomAttribute<ControllerFeatureAttribute>() is null)
                    continue;

                var file = codeGenerator.File($"{fileDef.Name}.g.cs");

                file
                    .WithUsing("System")
                    .WithUsing("using System.Collections.Generic;")
                    .WithUsing("using System.Linq;")
                    .WithUsing("using System.Text;")
                    .WithUsing("using System.IO;")
                    .WithUsing("using System.IO;")
                    .WithUsing("using Flurl;")
                    .WithUsing("using Flurl.Http;");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                var controllerName = typeDef.AsClassName();

                var type = ns.Type($"{controllerName}Client", MemberFlags.Public);

                type
                    .WithPrimaryConstructor()
                    .Property(MemberFlags.Public, CsharpTypeReference.ToRaw("FlurlClient"), "client");

                if (controllerName.EndsWith("Controller"))
                    controllerName = controllerName[..^"Controller".Length];

                foreach (var method in typeDef.Methods)
                {
                    var generatedMethod = type.Method(MemberFlags.Public | MemberFlags.Async, CsharpTypeReference.ToType(method.ResponseType), method.Name);

                    if (method.IsGetRequest())
                    {
                        generatedMethod.Body
                            .Append(method.IsVoidReturn() ? "" : "return ")
                            .Append("await client.Request")
                            .OpenParanthesis()
                            .Append(@$"""/api/{controllerName}/{method.Name}""")
                            .CloseParanthesis()
                            .Append(method.IsVoidReturn() ? ".GetAsync()" : $".GetJsonAsync<{method.ResponseType.UnwrapTask().Name}>()")
                            .Append(";");
                    }

                    if (method.IsPostRequest())
                    {
                        generatedMethod.Body
                            .Append("await client.Request")
                            .OpenParanthesis()
                            .Append(@$"""/api/{controllerName}/{method.Name}""")
                            .CloseParanthesis()
                            .Append(".PostJsonAsync()")
                            .Append(";");
                    }
                }

                //var method = type.Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString");

                //// TODO add support for methods to import namespaces;
                //method.Body.Append("return System.Text.Json.JsonSerializer.Serialize(this);");
            }
        }
    }
}

public static class ControllerClientModuleExtensions
{
    public static T WithControllerClientModuleGenerator<T>(this T project, Action<ControllerClientModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new ControllerClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}