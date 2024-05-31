using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class ExtensionsModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public ExtensionsModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            foreach (var typeDef in fileDef.Types)
            {
                var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

                var ns = file.Namespace(typeDef.GetNamespace());
                var type = ns.Type(typeDef.GetTypeName(), MemberFlags.Public, CsharpTypeReference.ToType(typeDef.Type));

                var extensions = GetTypeExtensions(typeDef.Type)
                    .Where(x => x != typeof(ValueType));

                foreach (var extension in extensions)
                    type.WithInheritsFrom(CsharpTypeReference.ToType(extension));
            }
        }
    }


    private static List<Type> GetTypeExtensions(Type type)
    {
        var extensions = new List<Type>();

        var baseType = type.BaseType;

        if (baseType != null && baseType != typeof(object))
            extensions.Add(baseType);

        var interfaces = type.GetInterfaces().Except(baseType?.GetInterfaces() ?? Enumerable.Empty<Type>());

        extensions.AddRange(interfaces);

        return extensions;
    }

}

public static class ExtensionsModuleExtensions
{
    public static T WithExtensionsGenerator<T>(this T project, Action<ExtensionsModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new ExtensionsModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}
