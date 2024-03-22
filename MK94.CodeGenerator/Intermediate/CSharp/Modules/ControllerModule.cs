using System;
using System.Linq;
using System.Reflection;
using MK94.CodeGenerator.Attributes;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class ControllerModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public ControllerModule(ICSharpProject project)
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

                file.WithUsing("Microsoft.AspNetCore.Mvc");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                var type = ns.Type(typeDef.AsClassName(), MemberFlags.Public);

                type
                    .Attribute(CsharpTypeReference.ToRaw("Route"))
                    .WithParam("api/[controller]/[action]");

                foreach (var method in typeDef.Methods)
                {
                    var generatedMethod = type.Method(MemberFlags.Public | MemberFlags.Partial, CsharpTypeReference.ToType(method.ResponseType), method.Name);

                    var getAttribute = method.MethodInfo.GetCustomAttribute<GetAttribute>();

                    if (getAttribute is not null)
                        generatedMethod.Attribute(CsharpTypeReference.ToRaw("HttpGet"));

                    var postAttribute = method.MethodInfo.GetCustomAttribute<PostAttribute>();

                    if (postAttribute is not null)
                        generatedMethod.Attribute(CsharpTypeReference.ToRaw("HttpPost"));
                }
            }
        }
    }
}

public static class ControllerModuleExtensions
{
    public static T WithControllerModuleGenerator<T>(this T project, Action<ControllerModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new ControllerModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}

public class ControllerFeatureAttribute : ProjectAttribute
{
    private const string Name = nameof(ControllerFeatureAttribute);

    public static Parser Parser = new Parser(new ParserConfig() { Project = Name, MandatoryFileAttribute = true });

    public ControllerFeatureAttribute() : base(Name) { }
}