using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator;

public class FileDefinition
{
    public FileAttribute FileInfo { get; set; }

    public string Name { get; set; }

    public List<EnumDefintion> EnumTypes { get; set; }

    public List<TypeDefinition> Types { get; set; }
}

public class EnumDefintion
{
    public Type Type { get; set; }

    public Dictionary<string, int> KeyValuePairs { get; set; }
}

public class PropertyDefinition
{
    public Type Type { get; set; }

    public string Name { get; set; }

    public PropertyInfo Info { get; set; }
}

public class TypeDefinition
{
    public Type Type { get; set; }

    public List<MethodDefinition> Methods { get; set; }

    public List<PropertyDefinition> Properties { get; set; }
}

public class MethodDefinition
{
    public string Name { get; set; }

    public Type ResponseType { get; set; }

    public MethodInfo MethodInfo { get; set; }

    public List<ParameterDefinition> Parameters { get; set; }
}

public class ParameterDefinition
{
    public Type Type { get; set; }

    public string Name { get; set; }

    public ParameterInfo Parameter { get; set; }
}

public class ParserConfig
{
    public string? Project { get; set; }

    public bool MandatoryFileAttribute { get; set; }
}

public class Parser
{
    private ParserConfig config;

    public Parser(ParserConfig config)
    {
        this.config = config;
    }

    public List<FileDefinition> ParseFromAssemblyContainingType<T>() => ParseFromAssembly(typeof(T).Assembly);

    public List<FileDefinition> ParseFromEntryAssembly() => ParseFromAssembly(Assembly.GetEntryAssembly()!);

    public List<FileDefinition> ParseFromAssembly(Assembly assembly)
    {
        var typesForProject = assembly
            .GetTypes()
            .ToDictionary(
                x => x,
                x => GetAttributeForCurrentProject(x))
            .Where(x => (config.Project == null && x.Key.GetCustomAttribute<FileAttribute>() != null) || x.Value != null);

        var typesGroupedByOutputFile = typesForProject.GroupBy(x => GetFilePath(x.Key), x => x.Key);

        return typesGroupedByOutputFile.Select(ParseFile).ToList();
    }

    public List<FileDefinition> ParseFromType(Type type)
    {
        var typesGroupedByOutputFile = new[] { type }.GroupBy(x => GetFilePath(x), x => x);

        return typesGroupedByOutputFile.Select(ParseFile).ToList();
    }

    private FileDefinition ParseFile(IGrouping<string, Type> types)
    {
        var allTypes = types.Where(x => !x.IsEnum);
        var enumTypes = types.Where(x => x.IsEnum);

        return new FileDefinition
        {
            Name = types.Key,
            EnumTypes = enumTypes.Select(ParseEnumType).ToList(),
            Types = allTypes.Select(ParseDataClass).ToList(),
        };
    }

    private EnumDefintion ParseEnumType(Type type)
    {
        return new EnumDefintion
        {
            Type = type,
            KeyValuePairs = Enum.GetValues(type)
                .Cast<int>()
                .ToDictionary(x => Enum.GetName(type, x)!, x => x)
        };
    }


    private MethodDefinition ParseApiMethod(MethodInfo method)
    {
        var parameters = method.GetParameters().Select(p => Tuple.Create(p, p.GetCustomAttribute<ParameterAttribute>())).ToList();

        return new MethodDefinition
        {
            Name = method.Name,
            MethodInfo = method,
            Parameters = parameters.Select(ParseParameter!).ToList(),
            ResponseType = method.ReturnType,
        };
    }

    private ParameterDefinition ParseParameter(Tuple<ParameterInfo, ParameterAttribute> arg)
    {
        return new ParameterDefinition
        {
            Name = arg.Item1.Name!,
            Type = arg.Item1.ParameterType,
            Parameter = arg.Item1
        };
    }

    private TypeDefinition ParseDataClass(Type type)
    {
        var parsedProps = new List<PropertyDefinition>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(PropertyEnabledForCurrentProject))
        {
            if (property.DeclaringType != type)
                continue;

            parsedProps.Add(new PropertyDefinition
            {
                Type = property.PropertyType,
                Name = property.Name,
                Info = property
            });
        }

        return new TypeDefinition
        {
            Type = type,
            Properties = parsedProps,
            Methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsSpecialName)
                .Select(ParseApiMethod)
                .ToList()
        };
    }

    private bool PropertyEnabledForCurrentProject(PropertyInfo property)
    {
        var onlyOnAttr = property.GetCustomAttributesUngrouped<OnlyOnAttribute>();

        if (onlyOnAttr.Any() && onlyOnAttr.All(a => a.Project != config.Project))
            return false;

        var projAttr = property.GetCustomAttributesUngrouped<ProjectAttribute>();

        if (projAttr.Any() && projAttr.All(p => p.Project != config.Project))
            return false;

        return true;
    }

    private ProjectAttribute? GetAttributeForCurrentProject(Type type)
    {
        return type
                .GetCustomAttributesUngrouped<ProjectAttribute>()
                .FirstOrDefault(p => config.Project != "*" && p.Project == config.Project);
    }

    private string GetFilePath(Type type)
    {
        var attr = type.GetCustomAttribute<FileAttribute>();

        if (config.MandatoryFileAttribute)
        {
            if (attr == null)
                throw new InvalidProgramException($"Type {type} is missing the File attribute");

            return attr.Name;
        }

        return type.Name;
    }
}

public class ParserV2
{
    public static void FromEntryAssembly() => new ParserV2(Assembly.GetEntryAssembly()!);

    private Assembly assembly;

    public ParserV2(Assembly assembly)
    {
        this.assembly = assembly;
    }

    public void FindTypesWithProperty<T>()
    {

    }
}
