using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class JsonToStringModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;
    private string Namespace = "Todo";

    public JsonToStringModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            foreach (var typeDef in fileDef.Types)
            {
                if (!typeDef.Properties.Any())
                    continue;

                var file = codeGenerator.File($"{fileDef.Name}.g.cs");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));
                var type = ns.Type(typeDef.Type.Name, MemberFlags.Public);

                var method = type.Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString");

                // TODO add support for methods to import namespaces;
                method.Body.Append("return System.Text.Json.JsonSerializer.Serialize(this);");
            }
        }
    }
}

public static class JsonToStringModuleExtensions
{
    public static T WithJsonToStringGenerator<T>(this T project, Action<JsonToStringModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new JsonToStringModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}