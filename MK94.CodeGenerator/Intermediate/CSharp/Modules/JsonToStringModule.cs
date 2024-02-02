using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class JsonToStringModule
{
    private readonly Project project;
    private string Namespace = "Todo";

    private JsonToStringModule(Project project)
    {
        this.project = project;
    }

    public static JsonToStringModule Using(Project project)
    {
        return new JsonToStringModule(project);
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File($"{fileDef.Name}.cs");

            foreach (var typeDef in fileDef.Types)
            {
                var ns = file.Namespace(Namespace);
                var type = ns.Type(typeDef.Type.Name, MemberFlags.Public);

                var method = type.Method(MemberFlags.Public | MemberFlags.Override, CsTypeReference.ToType<string>(), "ToString");

                // TODO add support for methods to import namespaces;
                method.Body.Append("return System.Text.Json.JsonSerializer.Serialize(this);");
            }
        }
    }
}
