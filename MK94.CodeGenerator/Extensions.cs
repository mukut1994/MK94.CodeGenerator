using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator
{
    public static class Extensions
    {
        public static IEnumerable<T> GetCustomAttributesUngrouped<T>(this MemberInfo memberInfo)
            where T : Attribute
        {
            foreach (var attr in memberInfo.GetCustomAttributes<T>())
                yield return attr;

            var typeAttr = memberInfo.GetCustomAttributes<GroupOfAttributes>();
            var propAttr = memberInfo.GetCustomAttributes<GroupOfPropertyAttributes>();

            foreach (var group in typeAttr)
            {
                foreach (var attr in group.Attributes)
                {
                    if (attr is T t)
                        yield return t;
                }
            }

            foreach (var group in propAttr)
            {
                foreach (var attr in group.Attributes)
                {
                    if (attr is T t)
                        yield return t;
                }
            }
        }

        public static bool FromBody(this ParameterDefinition p) => p.Parameter.GetCustomAttribute<BodyAttribute>() != null;
        public static bool FromForm(this ParameterDefinition p) => p.Parameter.GetCustomAttribute<FormAttribute>() != null;
        public static bool FromQuery(this ParameterDefinition p) => p.Parameter.GetCustomAttribute<QueryAttribute>() != null;

        public static int GetMessageCode(this MethodDefinition method)
        {
            return method.MethodInfo.GetCustomAttributesUngrouped<MessageCodeAttribute>().Single().Code;
        }

        public static HashSet<Type> GetImportedTypes(FileDefinition file)
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

            return propDefs
                .Concat(apiRetTypes)
                .Concat(apiArgTypes)
                .SelectMany(UnwrapCSharpTypes)
                .Concat(interfaces)
                .ToHashSet();
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

        public static IEnumerable<Type> ExpandBaseType(Type type)
        {
            if (type.BaseType == null)
                yield break;

            yield return type.BaseType;

            foreach (var x in ExpandBaseType(type.BaseType))
                yield return x;
        }

        public static IEnumerable<Type> FullyExpandType(Type type)
        {
            var set = new HashSet<Type> { type };

            foreach (var t in new[] { type }.Concat(ExpandBaseType(type)))
                foreach (var x in ExpandGenericType(t))
                    set.Add(x);

            return set;
        }

        public static IEnumerable<Type> UnwrapCSharpTypes(Type t)
        {
            if (t == typeof(Task))
                return new[] { typeof(void) };

            if (t.IsArray)
                return UnwrapCSharpTypes(t.GetElementType()!);

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

        public static IEnumerable<FileDefinition> Combine(this IEnumerable<FileDefinition> from, IEnumerable<FileDefinition> with)
        {
            return Combine(from, with, x => x.Name, (a, b) =>
            {
                return new FileDefinition
                {
                    Types = a.Types.Combine(b.Types).ToList(),
                    EnumTypes = a.EnumTypes.Combine(b.EnumTypes).ToList(),
                    FileInfo = a.FileInfo,
                    Name = a.Name
                };
            });
        }

        public static IEnumerable<TypeDefinition> Combine(this IEnumerable<TypeDefinition> from, IEnumerable<TypeDefinition> with)
        {
            return Combine(from, with, x => x.Type.FullName, (a, b) =>
            {
                return new TypeDefinition
                {
                    Type = a.Type,
                    Methods = a.Methods.Combine(b.Methods).ToList(),
                    Properties = a.Properties.Combine(b.Properties).ToList()
                };
            });
        }

        public static IEnumerable<MethodDefinition> Combine(this IEnumerable<MethodDefinition> from, IEnumerable<MethodDefinition> with)
        {
            return Combine(from, with, x => x.MethodInfo.Name, (a, b) => a);
        }

        public static IEnumerable<PropertyDefinition> Combine(this IEnumerable<PropertyDefinition> from, IEnumerable<PropertyDefinition> with)
        {
            return Combine(from, with, x => x.Name, (a, b) => a);
        }

        public static IEnumerable<EnumDefintion> Combine(this IEnumerable<EnumDefintion> from, IEnumerable<EnumDefintion> with)
        {
            return Combine(from, with, x => x.Type.FullName, (a, b) => a);
        }

        private static IEnumerable<T> Combine<T>(this IEnumerable<T> from, IEnumerable<T> with, Func<T, string> itemKey, Func<T, T, T> combine)
        {
            if (from == null)
                return with;

            if (with == null)
                return from;

            var ret = new List<T>();
            var withLookup = with.ToDictionary(itemKey, x => x);

            foreach (var item in from)
            {
                var fromKey = itemKey(item);

                var toItem = withLookup.GetValueOrDefault(fromKey);

                if (toItem == null)
                    ret.Add(item);
                else
                {
                    ret.Add(combine(item, toItem));
                    withLookup.Remove(fromKey);
                }
            }

            foreach (var withItem in withLookup)
            {
                ret.Add(withItem.Value);
            }

            return ret;
        }

        public static IEnumerable<FileDefinition> ExcludeAndInheritFrom(this IEnumerable<FileDefinition> from, IEnumerable<FileDefinition> baseFiles)
        {
            var ret = new List<FileDefinition>();
            var typesToExclude = baseFiles
                .SelectMany(x => x.Types.Select(x => x.Type).Concat(x.EnumTypes.Select(e => e.Type)))
                .ToHashSet();

            foreach (var file in from)
            {
                ret.Add(new FileDefinition
                {
                    Types = file.Types.Where(x => !typesToExclude.Contains(x.Type)).ToList(),
                    EnumTypes = file.EnumTypes.Where(x => !typesToExclude.Contains(x.Type)).ToList(),
                    FileInfo = file.FileInfo,
                    Name = file.Name
                });
            }

            return ret.Where(x => x.EnumTypes.Any() || x.Types.Any());
        }

        public static void GetDependencies(this IEnumerable<MethodDefinition> methods, DependencyLookupCache cache, ref HashSet<Type> dependencies)
        {
            var rets = methods.Select(x => x.ResponseType);
            var args = methods.SelectMany(x => x.Parameters.Select(p => p.Type));
            var attr = methods.SelectMany(x => x.MethodInfo.GetCustomAttributes<DependsOnAttribute>()).Select(x => x.Type);
            var websocket = methods.SelectMany(x => x.MethodInfo.GetCustomAttributesUngrouped<RepliesWithWebsocketAttribute>())
                .SelectMany(x => new[] { x.ReceiveType, x.ResponseType }.Where(x => x != null));

            foreach (var t in rets.Concat(args).Concat(attr).Concat(websocket).SelectMany(x => UnwrapCSharpTypes(x)))
            {
                if (dependencies.Contains(t))
                    continue;

                GetDependencies(cache, t, ref dependencies);
            }
        }

        public static void GetDependencies(DependencyLookupCache cache, Type type, ref HashSet<Type> dependencies)
        {
            dependencies.Add(type);

            if (cache.typeDefLookup.TryGetValue(type, out var tdef))
                tdef.Properties.GetDependencies(cache, ref dependencies);

            foreach (var t in FullyExpandType(type).Where(x => x != type))
                GetDependencies(cache, t, ref dependencies);

            if (type.BaseType != null && type.BaseType != typeof(Object) && type.BaseType != typeof(ValueType))
                GetDependencies(cache, type.BaseType, ref dependencies);
        }

        public record DependencyLookupCache(Dictionary<Type, FileDefinition> fileLookup, Dictionary<Type, TypeDefinition> typeDefLookup, Dictionary<Type, EnumDefintion> enumDefLookup);

        public static List<FileDefinition> ToFileDef(this HashSet<Type> types, DependencyLookupCache cache)
        {
            var ret = new Dictionary<string, FileDefinition>();

            foreach (var type in types)
            {
                if (!cache.fileLookup.TryGetValue(type, out var originalFileDef))
                    continue;

                if (!ret.TryGetValue(originalFileDef.Name, out var retDef))
                {
                    retDef = new FileDefinition
                    {
                        Name = originalFileDef.Name,
                        EnumTypes = new(),
                        FileInfo = originalFileDef.FileInfo,
                        Types = new()
                    };
                    ret.Add(originalFileDef.Name, retDef);
                }

                if (cache.typeDefLookup.TryGetValue(type, out var typeDef))
                    retDef.Types.Add(typeDef);

                if (cache.enumDefLookup.TryGetValue(type, out var enumDef))
                    retDef.EnumTypes.Add(enumDef);
            }

            return ret.Values.ToList();
        }

        public static HashSet<Type> GetMethodDependencies(this IEnumerable<FileDefinition> files, DependencyLookupCache cache)
        {
            var ret = new HashSet<Type>();

            foreach (var f in files)
            {
                foreach (var t in f.Types)
                {
                    t.Methods.GetDependencies(cache, ref ret);
                    t.Properties.GetDependencies(cache, ref ret);
                }
            }

            return ret;
        }

        public static void GetDependencies(this IEnumerable<ParameterDefinition> pars, DependencyLookupCache cache, ref HashSet<Type> dependencies)
        {
            foreach (var par in pars)
            {
                if (dependencies.Contains(par.Type))
                    continue;

                GetDependencies(cache, par.Type, ref dependencies);
            }
        }

        public static void GetDependencies(this IEnumerable<PropertyDefinition> props, DependencyLookupCache cache, ref HashSet<Type> dependencies)
        {
            foreach (var prop in props)
            {
                if (dependencies.Contains(prop.Type))
                    continue;

                GetDependencies(cache, prop.Type, ref dependencies);
            }
        }

        public static DependencyLookupCache BuildCache(this IEnumerable<FileDefinition> allFiles)
        {
            var tfileLookup = allFiles
                .SelectMany(x => x.Types.Select(y => (type: y.Type, file: x)));
            var efileLookup = allFiles
                .SelectMany(x => x.EnumTypes.Select(y => (type: y.Type, file: x)));

            var fileLookup = tfileLookup.Concat(efileLookup)
                .ToDictionary(x => x.type, x => x.file);
            var typeDefLookup = allFiles
                .SelectMany(x => x.Types.Select(y => (type: y.Type, typeDef: y)))
                .ToDictionary(x => x.type, x => x.typeDef);
            var enumDefLookup = allFiles
                .SelectMany(x => x.EnumTypes.Select(y => (type: y.Type, typeDef: y)))
                .ToDictionary(x => x.type, x => x.typeDef);

            return new DependencyLookupCache(fileLookup, typeDefLookup, enumDefLookup);
        }

        /*
        public static IEnumerable<FileDefinition> GetMethodDataDependencies(this IEnumerable<FileDefinition> methods, IEnumerable<FileDefinition> allFiles)
        {
            var importedTypes = methods.SelectMany(x => GetImportedTypes(x)).ToHashSet();
            var fileLookup = allFiles
                .SelectMany(x => x.Types.Select(y => (type: y.Type, file: x)))
                .ToDictionary(x => x.type, x => x.file);
            var typeDefLookup = allFiles
                .SelectMany(x => x.Types.Select(y => (type: y.Type, typeDef: y)))
                .ToDictionary(x => x.type, x => x.typeDef);

            var ret = new Dictionary<string, FileDefinition>();

            foreach(var type in importedTypes)
            {
                if (!fileLookup.TryGetValue(type, out var originalFileDef))
                    continue;

                if (!ret.TryGetValue(originalFileDef.Name, out var retDef))
                {
                    retDef = new FileDefinition
                    {
                        Name = originalFileDef.Name,
                        EnumTypes = new (),
                        FileInfo = originalFileDef.FileInfo,
                        Types = new ()
                    };
                    ret.Add(originalFileDef.Name, retDef);
                }

                var typeDef = typeDefLookup[type];
                retDef.Types.Add(typeDef);
            }

            return ret.Values;
        }*/

        public static IEnumerable<FileDefinition> ExcludeMethods(this IEnumerable<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (!file.EnumTypes.Any() && file.Types.All(x => !x.Properties.Any()))
                    continue;

                if (file.Types.All(x => !x.Methods.Any()))
                {
                    yield return file;
                    continue;
                }

                var ret = new FileDefinition
                {
                    Name = file.Name,
                    FileInfo = file.FileInfo,
                    EnumTypes = new(),
                    Types = file.Types.ExcludeMethods().ToList()
                };

                yield return ret;
            }
        }

        public static IEnumerable<FileDefinition> ExcludeAttribute<T>(this IEnumerable<FileDefinition> files)
            where T : Attribute
        {
            foreach (var file in files)
            {
                var ret = new FileDefinition
                {
                    Name = file.Name,
                    FileInfo = file.FileInfo,
                };

                ret.EnumTypes = file.EnumTypes.Where(x => x.Type.GetCustomAttribute<T>() == null).ToList();
                ret.Types = file.Types.Where(x => x.Type.GetCustomAttribute<T>() == null).ToList();

                if (ret.EnumTypes.Any() || ret.Types.Any())
                    yield return ret;
            }
        }

        public static IEnumerable<FileDefinition> ExcludeData(this IEnumerable<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (file.Types.All(x => !x.Methods.Any()))
                    continue;

                if (file.Types.All(x => !x.Properties.Any()) && !file.EnumTypes.Any())
                {
                    yield return file;
                    continue;
                }

                var ret = new FileDefinition
                {
                    Name = file.Name,
                    FileInfo = file.FileInfo,
                    EnumTypes = new(),
                    Types = file.Types.ExcludeMethods().ToList()
                };

                yield return ret;
            }
        }

        public static IEnumerable<TypeDefinition> ExcludeData(this IEnumerable<TypeDefinition> types)
        {
            foreach (var type in types)
            {
                if (!type.Methods.Any())
                    continue;

                if (!type.Properties.Any())
                    yield return type;

                yield return new TypeDefinition
                {
                    Methods = type.Methods,
                    Properties = new List<PropertyDefinition>(),
                    Type = type.Type
                };
            }
        }

        public static IEnumerable<TypeDefinition> ExcludeMethods(this IEnumerable<TypeDefinition> types)
        {
            foreach (var type in types)
            {
                if (!type.Properties.Any())
                    continue;

                if (!type.Methods.Any())
                    yield return type;

                yield return new TypeDefinition
                {
                    Methods = new(),
                    Properties = type.Properties,
                    Type = type.Type
                };
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TKey : notnull
            where TValue : new()
        {
            return dict.GetOrAdd(key, () => new TValue());
        }
         
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
            where TKey : notnull
        {
            TValue? value;

            if (!dict.TryGetValue(key, out value))
            {
                value = valueFactory();
                dict.Add(key, value);
            }

            return value;
        }
    }
}
