using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp;

public class CSharpProject : Project
{
    public Func<TypeDefinition, string> NamespaceResolver { get; set; } = _ => "NotSet";

    public string RelativePath {  get; set; }

    public CSharpProject WithNamespace(string @namespace)
    {
        NamespaceResolver = _ => @namespace;

        return this;
    }
}
