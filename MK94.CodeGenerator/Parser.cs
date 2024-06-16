using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator;

public class FeatureAttribute : Attribute
{
    /// <summary>
    /// The unique type to map this feature against. <br />
    /// Useful for creating thin helper attributes. <br />
    /// For example <see cref="GetAttribute" /> and  <see cref="PostAttribute" /> are mapped to <see cref="ControllerMethodAttribute"/>
    /// </summary>
    /// <returns></returns>
    public virtual Type GetFeatureType()
    {
        return GetType();
    }
}

public interface IFeatureMarked
{
    Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; }

    IEnumerable<IFeatureMarked> FeatureMarkedChildren { get; }

    IEnumerable<FeatureAttribute> ReadFeatures();
}

public class FileDefinition : IFeatureMarked
{
    [Obsolete]
    public FileAttribute FileInfo { get; set; }

    public string Name { get; set; }

    public List<EnumDefintion> EnumTypes { get; set; }

    public List<TypeDefinition> Types { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => EnumTypes.Cast<IFeatureMarked>().Concat(Types);

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        yield return new FileAttribute(Name);
    }
}

public class EnumDefintion : IFeatureMarked
{
    public Type Type { get; set; }

    public Dictionary<string, int> KeyValuePairs { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => Enumerable.Empty<IFeatureMarked>();

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        return Type.GetCustomAttributesUngrouped<FeatureAttribute>();
    }
}

public class PropertyDefinition : IFeatureMarked
{
    public Type Type { get; set; }

    public string Name { get; set; }

    public PropertyInfo Info { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => Enumerable.Empty<IFeatureMarked>();

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        return Info.GetCustomAttributesUngrouped<FeatureAttribute>();
    }
}

public class TypeDefinition : IFeatureMarked
{
    public Type Type { get; set; }

    public List<MethodDefinition> Methods { get; set; }

    public List<PropertyDefinition> Properties { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => Methods.Cast<IFeatureMarked>().Concat(Properties);

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        var ret = new Dictionary<Type, FeatureAttribute>();

        ret.Set(new TypeNameAttribute(Type.Name));

        foreach(var attr in Type.GetCustomAttributesUngrouped<FeatureAttribute>())
            ret.Set(attr);

        return ret.Values;
    }
}

public class MethodDefinition : IFeatureMarked
{
    public string Name { get; set; }

    public Type ResponseType { get; set; }

    public MethodInfo MethodInfo { get; set; }

    public List<ParameterDefinition> Parameters { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => Parameters;

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        return MethodInfo.GetCustomAttributesUngrouped<FeatureAttribute>();
    }
}

public class ParameterDefinition : IFeatureMarked
{
    public Type Type { get; set; }

    public string Name { get; set; }

    public ParameterInfo Parameter { get; set; }

    public Dictionary<Type, FeatureAttribute> FeatureMarks { get; set; } = new();

    public IEnumerable<IFeatureMarked> FeatureMarkedChildren => Enumerable.Empty<IFeatureMarked>();

    public IEnumerable<FeatureAttribute> ReadFeatures()
    {
        return Parameter.GetCustomAttributes<FeatureAttribute>();
    }
}

public class ParserConfig
{
    public string? Project { get; set; }
}

public class Parser
{
    private ParserConfig config;

    public Parser()
    {
        this.config = new ParserConfig();
    }

    public Parser(ParserConfig config)
    {
        if (config  == null) 
            throw new InvalidProgramException("Config for the parser must not be null. Either pass in a config or remove the `null` parameter being passed in.");

        this.config = config;
    }

    public List<FileDefinition> ParseFromTypes(params Type[] types)
    {
        var typesForProject = types
            .ToDictionary(
                x => x,
                x => GetAttributeForCurrentProject(x))
            .Where(x => (config.Project == null && x.Key.GetCustomAttribute<FileAttribute>() != null) || x.Value != null);

        return Parse(typesForProject.Select(x => (type: x.Key, path: GetFilePath(x.Key))));
    }

    public List<FileDefinition> ParseFromTypes(Func<Type, string> pathResolver, params Type[] types)
    {
        var typesForProject = types
            .ToDictionary(
                x => x,
                x => pathResolver(x))
            .Where(x => x.Value != null);

        return Parse(typesForProject.Select(x => (type: x.Key, path: x.Value)));
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

    public List<FileDefinition> Parse(IEnumerable<(Type type, string path)> types)
    {
        var typesGroupedByOutputFile = types
            .GroupBy(x => x.path, x => x.type);

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

        if (attr != null)
            return attr.Name;

        if (attr == null)
            throw new InvalidProgramException($"Type {type} is missing the File attribute");

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
