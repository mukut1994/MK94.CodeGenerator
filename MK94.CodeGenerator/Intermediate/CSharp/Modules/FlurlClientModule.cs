using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Generator;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class FlurlClientModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public ControllerResolver ControllerResolver { get; private set; } = ControllerResolver.Instance;

    public FlurlClientModule(ICSharpProject project)
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
                    .WithUsing("System.Collections.Generic")
                    .WithUsing("System.Linq")
                    .WithUsing("System.Text")
                    .WithUsing("System.IO")
                    .WithUsing("Flurl")
                    .WithUsing("Flurl.Http");

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

                    if (ControllerResolver.IsGetMethod(method))
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

                    if (ControllerResolver.IsPostMethod(method))
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
            }
        }
    }
}

public static class ControllerClientModuleExtensions
{
    public static T WithFlurlClientGenerator<T>(this T project, Action<FlurlClientModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new FlurlClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}