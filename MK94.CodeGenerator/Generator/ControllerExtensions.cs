using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MK94.CodeGenerator.Attributes;

namespace MK94.CodeGenerator.Generator;

public static class ControllerExtensions
{
    public static bool IsGetRequest(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<GetAttribute>().Any();
    public static bool IsPostRequest(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<PostAttribute>().Any();
    public static bool IsAnonymous(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<AnonymousAttribute>().Any();
    public static bool IsVoidReturn(this MethodDefinition m) => m.ResponseType == typeof(void) || m.ResponseType == typeof(Task);
    public static string Route(this MethodDefinition m) 
        => m.MethodInfo.GetCustomAttributesUngrouped<GetAttribute>().FirstOrDefault()?.Path ??
           $"api/{TypescriptClientGenerator.GetApiName(m.MethodInfo.DeclaringType)}/{m.Name}";

    public static Parameters Parameters(this MethodDefinition m)
    {
        var parameters = m.MethodInfo.GetParameters().Select(p => Tuple.Create(p, p.GetCustomAttribute<ParameterAttribute>())).ToList();

        return new Parameters
        {
            RouteParameters = parameters.Where(p => p.Item2 is RouteAttribute).Select(x => x.Item1.Name).ToList(),
            QueryParameters = parameters.Where(p => p.Item2 is QueryAttribute).Select(x => x.Item1.Name).ToList(),
            FormParameters = parameters.Where(p => p.Item2 is FormAttribute).Select(x => x.Item1.Name).ToList(),
            BodyParameters = parameters.Where(p => p.Item2 is BodyAttribute).Select(x => x.Item1.Name).ToList()
        };
    }

    public static List<string> RouteReplacements(this MethodDefinition m)
    {
        var route = m.Route();
        var parts = route.Split('/');

        var replacements = parts
            .Where(p => p.StartsWith('{') && p.EndsWith('}'))
            .Select(r => r.Substring(1, r.Length - 2))
            .ToList();

        return replacements;
    }
}