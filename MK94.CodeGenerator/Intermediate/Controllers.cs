using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate;


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

    public virtual bool IsQueryParameter(ParameterDefinition parameter)
    {
        return parameter.Parameter.GetCustomAttribute<QueryAttribute>(true) != null;
    }

    public virtual bool IsFormParameter(ParameterDefinition parameter)
    {
        return parameter.Parameter.GetCustomAttribute<FormAttribute>(true) != null;
    }
}

