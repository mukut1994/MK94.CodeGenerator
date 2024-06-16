using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using MK94.CodeGenerator;
using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

public class StronglyTypedAspNetCoreBindingModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public StronglyTypedAspNetCoreBindingModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

            foreach (var typeDef in fileDef.Types)
            {
                var attribute = typeDef.Type.GetCustomAttribute<StronglyTypedIdAttribute>();

                if (attribute == null) continue;

                var ns = file.Namespace(typeDef.GetNamespace());

                var originalType = ns.Type(typeDef.Type.Name, MemberFlags.Public, CsharpTypeReference.ToRaw(typeDef.Type.Name));

                originalType.Attribute(CsharpTypeReference.ToRaw("Microsoft.AspNetCore.Mvc.ModelBinder"))
                    .WithParam($"typeof({originalType.Name}Binder)");

                var converterClass = ns
                    .Type($"{originalType.Name}Binder", MemberFlags.Public, CsharpTypeReference.ToRaw($"{originalType.Name}Binder"))
                    .WithInheritsFrom(CsharpTypeReference.ToRaw($"Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinder"));

                if(attribute.Type == typeof(Guid))
                    ConvertMethod(converterClass, originalType.Name,
                        "Guid.TryParse(rawValue.FirstValue, out var parsed)", 
                        "guid");
                else if(attribute.Type == typeof(int))
                    ConvertMethod(converterClass, originalType.Name,
                        "int.TryParse(rawValue.FirstValue, out var parsed)",
                        "int");
                else
                    ConvertMethod(converterClass, originalType.Name, null, null);
            }
        }
    }

    private static void ConvertMethod(IntermediateTypeDefinition converterClass,
        string typeName,
        string? tryConvert,
        string? convertFailType)
    {
        converterClass.Method(MemberFlags.Public, CsharpTypeReference.ToRaw("Task"), "BindModelAsync")
                      .WithArgument(CsharpTypeReference.ToRaw("Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext"), "bindingContext")
                      .Body.Append($$"""
        var rawValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName); 

        if (rawValue.Length != 1)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Single value expected");
            bindingContext.Result = Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult.Failed();
        }
        {{
            (tryConvert == null ? string.Empty : $$"""
            else if (!{{tryConvert}})
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Invalid {{convertFailType}}");
                bindingContext.Result = Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult.Failed();
            } 
            """)
        }}
        else
        {
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, rawValue);
            bindingContext.Result = Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingResult.Success(new {{typeName}}({{ (tryConvert != null ? "parsed" : "rawValue.FirstValue!") }}));
        }

        return Task.CompletedTask;
        """);
    }
}

public static class StronglyTypedAspNetCoreBindingModuleExtensions
{
    public static T WithStronglyTypedAspnetBindingsGenerator<T>(this T project, Action<StronglyTypedAspNetCoreBindingModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new StronglyTypedAspNetCoreBindingModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}