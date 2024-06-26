﻿using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class FlurlClientModule : IGeneratorModule<CSharpCodeGenerator>
{
    public static HashSet<Type> QueryFriendlyTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        typeof(decimal),
        typeof(double),
        typeof(float),
        typeof(int),
        typeof(uint),
        typeof(nint),
        typeof(nuint),
        typeof(long),
        typeof(ulong),
        typeof(short),
        typeof(ushort),
        typeof(string),
        typeof(Guid),
    ];

    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public FlurlClientModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

            file.Usings.Add("Flurl");
            file.Usings.Add("System.Threading.Task");

            foreach (var typeDef in fileDef.Types)
            {
                var name = typeDef.GetTypeName();

                var ns = file.Namespace(typeDef.GetNamespace());
                var type = ns.Type(name, MemberFlags.Public, CsharpTypeReference.ToRaw(name));

                foreach (var methodDef in typeDef.Methods)
                {
                    var method = type.Method(MemberFlags.Public | MemberFlags.Static,
                        CsharpTypeReference.ToType(methodDef.ResponseType),
                        methodDef.Name);

                    foreach (var argDef in methodDef.Parameters)
                        method.WithArgument(CsharpTypeReference.ToType(argDef.Type), argDef.Name);

                    method.Body.AppendLine($@"return ""{method.Name}""");

                    SetQueryParams(methodDef, method);

                    method.Body.Append($@"  .ReceiveStringAsync();");
                }
            }
        }
    }

    private static void SetQueryParams(MethodDefinition methodDef, IntermediateMethodDefinition method)
    {
        foreach (var queryDef in methodDef.Parameters.Where(x => x.FromQuery()))
        {
            if(QueryFriendlyTypes.Contains(queryDef.Type))
            {
                method.Body.AppendLine($@"  .SetQueryParam(""{queryDef.Name}"", {queryDef.Name})");
                continue;
            }

            foreach(var property in queryDef.Parameter.ParameterType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var query = property.PropertyType.GetCustomAttributesUngrouped<QueryAttribute>();

                method.Body.AppendLine($@"  .SetQueryParam(""{property.Name.ToCamelCase()}"", {queryDef.Name}.{property.Name})");
            }
        }
    }
}

public static class FlurlClientModuleExtensions
{
    public static T WithFlurlClientGenerator<T>(this T project, Action<FlurlClientModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new FlurlClientModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}