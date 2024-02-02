﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class DataModule
{
    private readonly CSharpProject project;

    private DataModule(CSharpProject project)
    {
        this.project = project;
    }

    public static DataModule Using(CSharpProject project)
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
                var ns = file.Namespace(project.NamespaceResolver(typeDef));
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