using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class InterfaceMethodModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public InterfaceMethodModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            foreach (var typeDef in fileDef.Types)
            {
                if (!typeDef.Type.IsInterface)
                    continue;

                var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

                var ns = file.Namespace(typeDef.GetNamespace());
                var type = ns.Type(typeDef.GetTypeName(), MemberFlags.Public, CsharpTypeReference.ToRaw(typeDef.GetTypeName()));

                type.WithTypeAsInterface();

                foreach (var methodDef in typeDef.Methods)
                {
                    var method = type.Method(
                        MemberFlags.Public | MemberFlags.Interface,
                        CsharpTypeReference.ToType(methodDef.ResponseType),
                        methodDef.Name);

                    foreach (var paramDef in methodDef.Parameters)
                    {
                        method.WithArgument(CsharpTypeReference.ToType(paramDef.Type), paramDef.Name);
                    }
                }
            }
        }
    }
}

public static class InterfaceMethodModuleExtensions
{
    public static T WithInterfaceMethodGenerator<T>(this T project, Action<InterfaceMethodModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new InterfaceMethodModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}