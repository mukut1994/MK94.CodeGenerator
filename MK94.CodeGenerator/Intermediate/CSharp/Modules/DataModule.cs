using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class DataModule
{
    private readonly Project project;
    private string Namespace = "Todo";

    private DataModule(Project project)
    {
        this.project = project;
    }

    public static DataModule Using(Project project)
    {
        return new DataModule(project);
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            var file = codeGenerator.File($"{fileDef.Name}.cs");

            foreach(var typeDef in fileDef.Types)
            {
                var ns = file.Namespace(Namespace);
                var type = ns.Type(typeDef.Type.Name, MemberFlags.Public);

                foreach(var propertyDef in typeDef.Properties)
                {
                    type.Property(
                        MemberFlags.Public, 
                        CsTypeReference.ToType(propertyDef.Type),
                        propertyDef.Name);
                }
            }
        }
    }
}
