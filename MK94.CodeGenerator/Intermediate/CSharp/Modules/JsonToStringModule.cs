using MK94.CodeGenerator.Features;
using System;
using System.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class JsonToStringModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public JsonToStringModule(IFeatureGroup<CSharpCodeGenerator> project)
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

                var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

                var ns = file.Namespace(typeDef.GetNamespace());
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
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new JsonToStringModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}