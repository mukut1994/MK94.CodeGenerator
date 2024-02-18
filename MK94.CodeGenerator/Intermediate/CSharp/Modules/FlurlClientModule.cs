using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class FlurlClientModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public FlurlClientModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File($"{fileDef.Name}.cs");

            file.Usings.Add("Flurl");
            file.Usings.Add("System.Threading.Task");

            foreach (var typeDef in fileDef.Types)
            {
                var name = typeDef.Type.Name.StartsWith('I') && typeDef.Type.IsInterface ?
                    typeDef.Type.Name[1..] : typeDef.Type.Name;

                var ns = file.Namespace(project.NamespaceResolver(typeDef));
                var type = ns.Type(name, MemberFlags.Public);

                foreach (var methodDef in typeDef.Methods)
                {
                    var method = type.Method(MemberFlags.Public | MemberFlags.Static,
                        CsharpTypeReference.ToType(methodDef.ResponseType),
                        methodDef.Name);

                    foreach(var argDef in methodDef.Parameters)
                        method.WithArgument(CsharpTypeReference.ToType(argDef.Type), argDef.Name);

                    method.Body.AppendLine($@"return ""{method.Name}""");

                    foreach(var queryDef in methodDef.Parameters.Where(x => x.FromQuery()))
                        method.Body.AppendLine($@"  .SetQueryParam(""{queryDef.Name}"", {queryDef.Name})");

                    method.Body.Append($@"  .ReceiveStringAsync();");
                }
            }
        }
    }
}

public static class FlurlClientModuleExtensions
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