using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator;


internal static class CSharpHelper
{
    static NullabilityInfoContext nullableContext = new NullabilityInfoContext();

    public static string CSharpName(Type type, ParameterInfo info)
    {
        return CSharpName(type, nullableContext.Create(info));
    }

    public static string CSharpName(Type type, PropertyInfo info)
    {
        return CSharpName(type, nullableContext.Create(info));
    }

    public static string CSharpName(Type type, MethodDefinition info)
    {
        var n = nullableContext.Create(info.MethodInfo.ReturnParameter);

        if (info.ResponseType.IsGenericType && info.ResponseType.GetGenericTypeDefinition() == typeof(Task<>))
            n = n.GenericTypeArguments[0];

        return CSharpName(type, n);
    }

    public static string CSharpName(Type type, NullabilityInfo? nullable = null)
    {
        if (type == typeof(void))
            return "void";
        if (type == typeof(IFileResult))
            return "byte[]";
        if (type == typeof(JsonDocument))
            return "System.Text.Json.JsonDocument";

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            type = type.GetGenericArguments()[0];

        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().Name;
            var genericPart = type.GetGenericArguments()
                .Select((g, i) => CSharpName(g, nullable?.GenericTypeArguments[i]))
                .Aggregate((a, b) => $"{a}, {b}");

            var ret = MarkNullable($"{name.Substring(0, name.IndexOf('`'))}<{genericPart}>", type, nullable);

            return ret;
        }

        return MarkNullable(type.Name, type, nullable);
    }

    public static bool IsNullable(PropertyInfo info)
    {
        var nullable = nullableContext.Create(info);

        return IsNullable(nullable);
    }

    public static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsNullable(NullabilityInfo info)
    {
        if (info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable)
            return true;

        if (info.Type.IsGenericType && info.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;

        return false;
    }

    private static string MarkNullable(string text, Type type, NullabilityInfo? info)
    {
        if (IsNullable(type))
            return text + "?";

        if (info == null)
            return text;

        if (IsNullable(info))
            return text + "?";

        return text;
    }

    public static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            return type.GetGenericArguments()[0];

        return type;
    }

    private const byte NonNullableContextValue = 1;
    private const byte NullableContextValue = 2;

    public static bool IsReferenceTypePropertyNullable(this PropertyInfo property)
    {
        var classNullableContextAttribute = property.DeclaringType!.CustomAttributes
           .FirstOrDefault(c => c.AttributeType.Name == "NullableContextAttribute");


        var classNullableContext = classNullableContextAttribute
            ?.ConstructorArguments
            .First(ca => ca.ArgumentType.Name == "Byte")
            .Value;

        // EDIT: This logic is not correct for nullable generic types
        var propertyNullableContext = property.CustomAttributes
            .FirstOrDefault(c => c.AttributeType.Name == "NullableAttribute")
            ?.ConstructorArguments
            .First(ca => ca.ArgumentType.Name == "Byte")
            .Value;

        // If the property does not have the nullable attribute then it's 
        // nullability is determined by the declaring class 
        propertyNullableContext ??= classNullableContext;

        // If NullableContextAttribute on class is not set and the property
        // does not have the NullableAttribute, then the proeprty is non nullable
        if (propertyNullableContext == null)
        {
            return false;
        }

        // nullableContext == 0 means context is null oblivious (Ex. Pre C#8)
        // nullableContext == 1 means not nullable
        // nullableContext == 2 means nullable
        switch (propertyNullableContext)
        {
            case NonNullableContextValue:
                return false;
            case NullableContextValue:
                return true;
            default:
                throw new Exception("My error message");
        }
    }
}
