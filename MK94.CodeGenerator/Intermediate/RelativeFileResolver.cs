using MK94.CodeGenerator.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate;

public class RelativeFileResolver
{
    private readonly HashSet<Type> definedTypes;
    // hacky fix because it's not part of feature groups
    internal List<FileDefinition> files;

    private static readonly Dictionary<Type, string> externalTypes = new();

    public RelativeFileResolver(List<FileDefinition> files)
    {
        this.files = files.SelectMany(SplitDataAndApiTypes).ToList();
        definedTypes = files.SelectMany(GetImportableTypes).Concat(externalTypes.Keys).ToHashSet();
    }

    public virtual HashSet<Type> GetImports(FileDefinition file)
    {
        var toImport = GetImportedTypes(file);
        toImport.IntersectWith(definedTypes);

        return toImport;
    }

    public virtual string? GetImportPath(string path, Type import)
    {
        var importFileTarget = FindTypeInFiles(import, files);

        if (importFileTarget == null)
        {
            if (externalTypes.TryGetValue(import, out var importLocation))
                return importLocation;

            return null;
        }

        var relative = Path.GetRelativePath(path, importFileTarget.GetFilename());

        if (relative == ".") // same file
            return null;


        // TODO disabled because it's bugged
        // return null;

        relative = relative.Replace(".ts", "");
        relative = relative.Replace("\\", "/");
        relative = relative.Substring(3, relative.Length - 3);
        return $"./{relative}";
    }

    protected FileDefinition? FindTypeInFiles(Type type, List<FileDefinition> files)
    {
        var matches = files.Where(f => f.Types.Any(d => d.Type == type) || f.EnumTypes.Any(e => e.Type == type));

        if (matches.Count() == 0)
            return null;

        if (matches.Count() > 1)
            throw new InvalidProgramException($"Type {type.FullName} exists multiple times in project; {matches.Select(x => x.Name).Aggregate((a, b) => $"{a},{b}")}");

        return matches.Single();
    }

    protected virtual bool RequiresImport(Type type)
    {
        if (type == typeof(byte)) return false;
        if (type == typeof(int)) return false;
        if (type == typeof(decimal)) return false;
        if (type == typeof(bool)) return false;
        if (type == typeof(string)) return false;
        if (type == typeof(Nullable<>)) return false;
        if (type == typeof(DateTime)) return false;
        if (type == typeof(TimeSpan)) return false;
        if (type == typeof(DateTime?)) return false;
        if (type == typeof(Guid)) return false;
        if (type == typeof(Task)) return false;
        if (type == typeof(Task<>)) return false;
        if (type == typeof(List<>)) return false;
        if (type == typeof(Task<Guid>)) return false;
        if (type == typeof(Enum)) return false;
        if (type.Name == "IFileData") return false;
        if (type == typeof(System.IO.Stream)) return false;
        if (type == typeof(object)) return false;
        if (type == typeof(byte[])) return false;

        return true;
    }

    private IEnumerable<FileDefinition> SplitDataAndApiTypes(FileDefinition file)
    {
        if (file.Types.Any(x => x.Properties.Any())
            || file.EnumTypes.Any()
            || file.Name == "Ids") // TODO hacky fix
        {
            yield return new FileDefinition
            {
                Name = $"{file.Name}.ts",
                FileInfo = file.FileInfo,
                EnumTypes = file.EnumTypes,
                Types = file.Types.Select(x => new TypeDefinition
                {
                    Type = x.Type,
                    Properties = x.Properties,
                    Methods = new List<MethodDefinition>()
                }).ToList()
            };
        }

        if (file.Types.Any(x => x.Methods.Any()))
        {
            yield return new FileDefinition
            {
                Name = $"{file.Name}.ts",
                FileInfo = file.FileInfo,
                Types = file.Types.Select(x => new TypeDefinition
                {
                    Type = x.Type,
                    Methods = x.Methods,
                    Properties = new List<PropertyDefinition>()
                }).ToList(),
                EnumTypes = new List<EnumDefintion>()
            };
        }
    }

    private HashSet<Type> GetImportableTypes(FileDefinition file)
    {
        var enumDefs = from e in file.EnumTypes
                       select e.Type;

        var propDefs = from type in file.Types
                       from propDef in type.Properties
                       select propDef.Type;

        var apiRetTypeDefs = from apiClass in file.Types
                             from method in apiClass.Methods
                             select method.ResponseType;

        var apiArgTypes = from api in file.Types
                          from method in api.Methods
                          from arg in method.Parameters
                          select arg.Type;

        var allTypeDefs = file.Types.Select(x => x.Type)
            .Concat(enumDefs)
            .Concat(propDefs)
            .Concat(apiArgTypes)
            .Concat(apiRetTypeDefs)
            .SelectMany(ExpandGenericType)
            .Where(RequiresImport);

        return allTypeDefs.ToHashSet();
    }

    private static HashSet<Type> GetImportedTypes(FileDefinition file)
    {
        var propDefs = from type in file.Types
                       from propDef in type.Properties
                       select propDef.Type;

        propDefs = propDefs.Select(x => x.IsArray ? x.GetElementType() : x);

        var interfaces = propDefs.SelectMany(x => x.GetInterfaces());

        var apiRetTypes = from api in file.Types
                          from method in api.Methods
                          from eUnwrap in UnwrapCSharpTypes(method.ResponseType)
                          from e in ExpandGenericType(eUnwrap)
                          select e;

        var apiArgTypes = from api in file.Types
                          from method in api.Methods
                          from arg in method.Parameters
                          from e in ExpandGenericType(arg.Type)
                          select e;

        var extensionTypes = from type in file.Types
                             from x in Extensions.FullyExpandType(type.Type)
                             select x;

        return propDefs
            .Concat(apiRetTypes)
            .Concat(apiArgTypes)
            .Concat(extensionTypes)
            .SelectMany(Extensions.FullyExpandType)
            .SelectMany(UnwrapCSharpTypes)
            .Concat(interfaces)
            .ToHashSet();
    }

    // TODO move to a more generic helper

    private static IEnumerable<Type> UnwrapCSharpTypes(Type t)
    {
        if (t == typeof(Task))
            return new[] { typeof(void) };

        else if (!t.IsGenericType || t.IsGenericTypeDefinition)
            return new[] { t };


        var genericDef = t.GetGenericTypeDefinition();

        if (genericDef == typeof(Task<>))
            return UnwrapCSharpTypes(t.GenericTypeArguments[0]);

        if (genericDef == typeof(List<>))
            return UnwrapCSharpTypes(t.GenericTypeArguments[0]);

        if (genericDef == typeof(Nullable<>))
            return UnwrapCSharpTypes(t.GenericTypeArguments[0]);

        if (genericDef == typeof(Dictionary<,>))
            return UnwrapCSharpTypes(t.GenericTypeArguments[0]).Concat(UnwrapCSharpTypes(t.GenericTypeArguments[1]));

        return new[] { t };
    }

    public static HashSet<Type> ExpandGenericType(Type type)
    {
        var expandedSet = new HashSet<Type>();

        void expandType(Type type)
        {
            if (type.IsGenericTypeParameter)
                return;

            if (!type.IsGenericType)
            {
                expandedSet.Add(type);
                return;
            }

            foreach (var genericType in type.GetGenericArguments())
                expandType(genericType);

            expandedSet.Add(type.GetGenericTypeDefinition());
        }

        expandType(type);

        return expandedSet;
    }
}
