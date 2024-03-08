using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MK94.CodeGenerator.Generator.CSharpHelper;

namespace MK94.CodeGenerator.Generator;

public class CSharpDataGenerator
{
    private readonly bool AspNetCompatability;

    public CSharpDataGenerator(bool aspNetCompatability = false)
    {
        AspNetCompatability = aspNetCompatability;
    }

    public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files, params string[] additionalNamespaces)
    {
        foreach (var file in files)
        {
            var output = builderFactory(file.Name + ".cs");
            Generate(output, @namespace, file, additionalNamespaces);
            output.Flush();
        }
    }

    public void Generate(CodeBuilder builder, string @namespace, FileDefinition fileDefinition, params string[] additionalNamespaces)
    {
        builder.AppendUsings("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Numerics");

        foreach (var n in additionalNamespaces)
            builder.AppendUsings(n);

        builder
            .AppendNamespace(@namespace)
            .WithBlock(Generate, fileDefinition);

        builder.Flush();
    }

    private void Generate(CodeBuilder builder, FileDefinition fileDefinition)
    {
        builder
            .Append(Generate, fileDefinition.Types)
            .Append(Generate, fileDefinition.EnumTypes);
    }

    private void Generate(CodeBuilder builder, EnumDefintion e)
    {
        builder
            .AppendLine($"public enum {e.Type.Name}")
            .WithBlock((b, i) => b.AppendLine($"{i.Key} = {i.Value},"), e.KeyValuePairs);
    }

    private void Generate(CodeBuilder builder, TypeDefinition type)
    {
        builder
               .AppendLine($"public {(type.Type.IsInterface ? "interface" : "class")} {CSharpName(type.Type)}{Extensions(type)}")
               .OpenBlock()
               .Append((x, p) => Generate(x, p, !type.Type.IsInterface), type.Properties)
               .Append(Generate, type.Methods)
               .CloseBlock();
    }

    private void Generate(CodeBuilder builder, MethodDefinition method)
    {
        builder
               .Append($"{CSharpName(method.ResponseType, method.MethodInfo.ReturnParameter)} {method.Name}")
               .WithParenthesis(Generate, method.Parameters)
               .AppendLine(";");
    }

    private void Generate(CodeBuilder builder, ParameterDefinition def)
    {
        builder
            .Append($"{CSharpName(def.Type, def.Parameter)} {def.Name}")
            .AppendOptionalComma();
    }

    private static string Extensions(TypeDefinition type)
    {
        var extensions = GetTypeExtensions(type.Type).Select(x => CSharpName(x));

        if (!extensions.Any())
            return string.Empty;

        return $" : {extensions.Aggregate((a, b) => $"{a}, {b}")}";
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

    private void Generate(CodeBuilder builder, PropertyDefinition property, bool isOnClass)
    {
        if (AspNetCompatability && property.Info.GetCustomAttribute<QueryAttribute>() != null)
        {
            builder.AppendLine("[Microsoft.AspNetCore.Mvc.FromQuery]");
        }

        if (isOnClass && !IsNullable(property.Info))
            builder.Append("public required ");
        else
            builder.Append("public ");

        builder
            .AppendLine($"{CSharpName(property.Type, property.Info)} {property.Name} {{ get; set; }}");
    }

}
