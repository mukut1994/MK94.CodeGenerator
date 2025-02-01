using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Generator;
using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class FetchClientModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly IFeatureGroup<TypescriptCodeGenerator> project;

    public ControllerResolver ControllerResolver { get; private set; } = ControllerResolver.Instance;

    public FetchClientModule(IFeatureGroup<TypescriptCodeGenerator> project)
    {
        this.project = project;
    }

    public FetchClientModule WithResolver(ControllerResolver resolver)
    {
        ControllerResolver = resolver;
        return this;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            foreach(var typeDef in fileDef.Types)
            {
                if (!typeDef.Methods.Any())
                    continue;

                var file = codeGenerator.File($"{fileDef.GetFilename()}.ts");

                var type = file.Type(typeDef.GetTypeName(), MemberFlags.Public);

                foreach(var methodDef in typeDef.Methods)
                {
                    var method = type.Method(MemberFlags.Public | MemberFlags.Static | MemberFlags.Async,
                        TsTypeReference.ToPromiseType(methodDef.ResponseType),
                        methodDef.Name);

                    method.WithArgument(TsTypeReference.ToAnonymous(), "f", "fetch");
                    ParameterDefinition? bodyParam = null;

                    foreach (var argDef in methodDef.Parameters)
                    {
                        method.WithArgument(TsTypeReference.ToType(argDef.Type), argDef.Name);

                        var isBodyParam = ControllerResolver.IsBodyParameter(argDef);

                        if (isBodyParam)
                            bodyParam = argDef;
                    }

                    method.WithArgument(TsTypeReference.ToNamed(null, "RequestInit"), "init?");

                    var queryArgs = AppendCallParams(method.Body, methodDef, methodDef.Parameters);
                    var hasForm = AppendFormData(method.Body, methodDef, methodDef.Parameters);

                    if (hasForm || bodyParam != null || ControllerResolver.IsPostMethod(methodDef))
                    {
                        method.Body.Append("init = {")
                                .IncreaseIndent()
                                    .NewLine()
                                    .AppendLine("...init,");

                        if (ControllerResolver.IsPostMethod(methodDef))
                            method.Body.AppendLine("method: \"POST\",");

                        if (bodyParam != null)
                        {
                            method.Body
                                .AppendLine(@"headers: {...init?.headers, ""Content-Type"": ""application/json"" },")
                                .AppendLine($"body: JSON.stringify({bodyParam.Name}),");
                        }

                        else if (hasForm)
                            method.Body.AppendLine($"body: _form,");

                        method.Body
                                .DecreaseIndent()
                                .AppendLine("};")
                                .NewLine();
                    }

                    method.Body.AppendLine($@"const ret = await f(""{typeDef.AsApiName()}/{method.Name}{UrlSearchParams(queryArgs)}, init);");

                    method.Body.Append($@"return ret.json();");
                }
            }
        }
    }

    private string UrlSearchParams(bool enable)
    {
        if (!enable)
            return @"""";

        return $@"?"" + new URLSearchParams(_params).toString()";
    }

    private bool AppendFormData(CodeBuilder builder, MethodDefinition method, List<ParameterDefinition> paramaters)
    {
        var formArgs = paramaters.SelectMany(parameter =>
            ControllerResolver
                .GetFormParameters(parameter)
                .Select(queryArg => (parameter, queryArg))).ToList();

        if (!formArgs.Any())
            return false;

        builder
            .AppendLine($"const _form = new FormData();")
            .NewLine();

        foreach (var p in formArgs)
        {
            var expression = p.parameter.Name;

            if (p.queryArg.property.Any())
                expression =p.queryArg.property
                .Select(x => x.ToCamelCase())
                .Aggregate((a, b) => $"{a}?.{b}");

            builder
                .AppendLine($"if ({expression} !== undefined && {expression} !== null) _form.append(\"{p.queryArg.key}\", {expression}.toString());");
        }

        builder.NewLine();

        return true;
    }

    private bool AppendCallParams(CodeBuilder builder, MethodDefinition method, List<ParameterDefinition> paramaters)
    {
        var queryArgs = paramaters.SelectMany(parameter => 
            ControllerResolver
                .GetQueryParameters(parameter)
                .Select(queryArg => (parameter, queryArg))).ToList();

        if (!queryArgs.Any())
            return false;

        builder
            .AppendLine($"const _params: Record<string, string> = {{}};")
            .NewLine();

        foreach (var p in queryArgs)
        {
            var expression = p.parameter.Name;

            if (p.queryArg.property.Any())
                expression = p.queryArg.property
                .Select(x => x.ToCamelCase())
                .Aggregate((a, b) => $"{a}?.{b}");

            builder
                .AppendLine($"if ({expression} !== undefined && {expression} !== null) _params[\"{p.queryArg.key}\"] = {expression}.toString();");
        }

        builder.NewLine();

        return true;
    }
}

public static class FetchClientModuleModuleExtensions
{
    public static T WithFetchClientModuleGenerator<T>(this T project, Action<FetchClientModule>? configure = null)
        where T : IFeatureGroup<TypescriptCodeGenerator>
    {
        var mod = new FetchClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}