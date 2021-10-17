using MK94.DataGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator
{
    public class FileDefinition
    {
        public string Name { get; set; }

        public List<EnumDefintion> EnumTypes { get; set; }

        public List<DataDefinition> DataClasses { get; set; }

        public List<ApiDefinition> ApiClasses { get; set; }
    }

    public class EnumDefintion
    {
        public Type Type { get; set; }

        public Dictionary<string, int> KeyValuePairs { get; set; }
    }

    public class DataDefinition
    {
        public Type Type { get; set; }

        public List<PropertyDefinition> Properties { get; set; }
    }

    public class PropertyDefinition
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public PropertyInfo Info { get; set; }
    }

    public class ApiDefinition
    {
        public Type Type { get; set; }

        public List<ApiEndpoint> Methods { get; set; }
    }

    public class ApiEndpoint
    {
        public string Name { get; set; }

        public Type ResponseType { get; set; }

        public List<ParameterDefinition> Parameters { get; set; }
    }

    public class ParameterDefinition
    {
        public Type Type { get; set; }

        public string Name { get; set; }
    }

    public class Parser
    {
        private string project;

        public Parser(string project)
        {
            this.project = project;
        }

        public List<FileDefinition> ParseFromType(Type type)
        {
            var typesGroupedByOutputFile = new[] { type }.GroupBy(x => GetFilePath(x), x => x);

            return typesGroupedByOutputFile.Select(ParseFile).ToList();
        }

        public List<FileDefinition> ParseFromAssembly(Assembly assembly)
        {
            var typesForProject = assembly
                .GetTypes()
                .ToDictionary(
                    x => x,
                    x => GetAttributeForCurrentProject(x))
                .Where(x => x.Value != null);

            var typesGroupedByOutputFile = typesForProject.GroupBy(x => GetFilePath(x.Key), x => x.Key);

            return typesGroupedByOutputFile.Select(ParseFile).ToList();
        }

        private FileDefinition ParseFile(IGrouping<string, Type> types)
        {
            var apiTypes = types.Where(IsApiType).ToList();
            var enumTypes = types.Except(apiTypes).Where(x => x.IsEnum).ToList();
            var dataTypes = types.Except(apiTypes).Except(enumTypes).ToList();

            return new FileDefinition
            {
                Name = types.Key,
                EnumTypes = enumTypes.Select(ParseEnumType).ToList(),
                DataClasses = dataTypes.Select(ParseDataClass).ToList(),
                ApiClasses = apiTypes.Select(ParseApiClass).ToList()
            };
        }

        private EnumDefintion ParseEnumType(Type type)
        {
            return new EnumDefintion
            {
                Type = type,
                KeyValuePairs = Enum.GetValues(type)
                    .Cast<int>()
                    .ToDictionary(x => Enum.GetName(type, x), x => x)
            };
        }

        private ApiDefinition ParseApiClass(Type type)
        {
            return new ApiDefinition
            {
                Type = type,
                Methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(ParseApiMethod)
                    .ToList()
            };
        }

        private ApiEndpoint ParseApiMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();

            return new ApiEndpoint
            {
                Name = method.Name,
                Parameters = parameters.Select(ParseParameter).ToList(),
                ResponseType = method.ReturnType
            };
        }

        private ParameterDefinition ParseParameter(ParameterInfo param)
        {
            return new ParameterDefinition
            {
                Name = param.Name,
                Type = param.ParameterType
            };
        }

        private DataDefinition ParseDataClass(Type type)
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

            return new DataDefinition
            {
                Type = type,
                Properties = parsedProps
            };
        }

        private bool PropertyEnabledForCurrentProject(PropertyInfo property)
        {
            var onlyOnAttr = property.GetCustomAttributesUngrouped<OnlyOnAttribute>();

            if (onlyOnAttr.Any() && onlyOnAttr.All(a => a.Feature != project))
                return false;

            var projAttr = property.GetCustomAttributesUngrouped<ProjectAttribute>();

            if (projAttr.Any() && projAttr.All(p => p.Project != project))
                return false;

            return true;
        }

        private ProjectAttribute GetAttributeForCurrentProject(Type type)
        {
            return type.GetCustomAttributesUngrouped<ProjectAttribute>().FirstOrDefault(p => p.Project == project);
        }

        private string GetFilePath(Type type)
        {
            var attr = type.GetCustomAttribute<FileAttribute>();

            if (attr == null)
                throw new InvalidProgramException($"Type {type} is missing the File attribute");

            return attr.Name;
        }

        private static bool IsApiType(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (type.IsEnum)
                return false;

            else if (type.Name.EndsWith("Controller"))
            {
                // TODO these checks should go into a roslyn code analyser
                if (properties.Any())
                    throw new InvalidProgramException($"Controller type {type.FullName} is not allowed to have properties");
                if (!methods.Any())
                    throw new InvalidProgramException($"Controller type {type.FullName} has no methods");

                return true;
            }
            else
            {
                if (!properties.Any())
                    throw new InvalidProgramException($"Controller type {type.FullName} has no properties");

                return false;
            }
        }
    }
}
