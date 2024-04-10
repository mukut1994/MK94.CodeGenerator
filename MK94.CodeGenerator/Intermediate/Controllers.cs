using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate;

public record PropertyArgumentExpression(string key, params string[] property);

public class ControllerResolver
{
    public static ControllerResolver Instance { get; } = new ControllerResolver();

    protected ControllerResolver() { }

    public virtual bool IsControllerMethod(MethodDefinition method)
    {
        return method.MethodInfo.GetCustomAttributes<ControllerMethodAttribute>(true) != null;
    }

    public virtual bool IsGetMethod(MethodDefinition method)
    {
        return method.MethodInfo.GetCustomAttributes<GetAttribute>(true) != null;
    }

    public virtual bool IsPostMethod(MethodDefinition method)
    {
        return method.MethodInfo.GetCustomAttributes<PostAttribute>(true) != null;
    }

    public virtual bool IsBodyParameter(ParameterDefinition parameter)
    {
        return parameter.Parameter.GetCustomAttribute<BodyAttribute>(true) != null;
    }

    public virtual IEnumerable<PropertyArgumentExpression> GetQueryParameters(ParameterDefinition parameter)
    {
        return GetPropertiesWithAttribute<QueryAttribute>(parameter);
    }

    public virtual IEnumerable<PropertyArgumentExpression> GetFormParameters(ParameterDefinition parameter)
    {
        return GetPropertiesWithAttribute<FormAttribute>(parameter);
    }

    protected IEnumerable<PropertyArgumentExpression> GetPropertiesWithAttribute<T>(ParameterDefinition parameter)
        where T : Attribute
    {
        var rootAttr = parameter.Parameter.GetCustomAttribute<T>();

        if (rootAttr != null)
        {
            yield return new(parameter.Name, parameter.Name);
            yield break;
        }

        foreach (var prop in parameter.Parameter.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attr = prop.GetCustomAttribute<T>();

            if (attr == null)
                continue;

            yield return new(prop.Name, prop.Name);
        }
    }

    public virtual bool IsFormParameter(ParameterDefinition parameter)
    {
        return parameter.Parameter.GetCustomAttribute<FormAttribute>(true) != null;
    }
}

