using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator
{
    public class Parameters
    {
        public List<string> RouteParameters { get; set; }
        public List<string> QueryParameters { get; set; }
        public List<string> FormParameters { get; set; }
        public List<string> BodyParameters { get; set; }
    }

    public static class ExtensionsController
    {
        public static bool IsGetRequest(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<GetAttribute>().Any();
        public static bool IsPostRequest(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<PostAttribute>().Any();
        public static bool IsAnonymous(this MethodDefinition m) => m.MethodInfo.GetCustomAttributesUngrouped<AnonymousAttribute>().Any();

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

    public class TypescriptClientGenerator
    {
        private readonly HashSet<Type> definedTypes;
        private readonly List<FileDefinition> files;

        private static readonly Dictionary<Type, string> systemTypes = new()
        {
            { typeof(Quaternion), "three" }
        };

        public TypescriptClientGenerator(List<FileDefinition> files)
        {
            this.files = files.SelectMany(SplitDataAndApiTypes).ToList();
            definedTypes = files.SelectMany(GetImportableTypes).Concat(systemTypes.Keys).ToHashSet();
        }

        public void Generate(Func<string, CodeBuilder> builderFactory)
        {
            foreach (var file in files)
            {
                var output = builderFactory(file.Name);

                if (file.Name.StartsWith("model"))                
                    GenerateModel(output, file); 
                else                
                    GenerateApi(output, file);                

                output.Flush();
            }
        }

        private IEnumerable<FileDefinition> SplitDataAndApiTypes(FileDefinition file)
        {
            if (file.Types.Any(x => x.Properties.Any()) 
                || file.EnumTypes.Any()
                || file.Name == "Ids") // TODO hacky fix
            {
                yield return new FileDefinition
                {
                    Name = $"models/{file.Name}.model.ts",
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
                    Name = $"services/{file.Name}.api.ts",
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

        private void GenerateApi(CodeBuilder builder, FileDefinition file)
        {
            builder
                .AppendLine("/*")
                .AppendLine("    GENERATED FILE")
                .AppendLine("    Any changes you make to this file by hand are going to be overwritten by MK94.CodeGenerator.Generator and the code generator there.")
                .AppendLine("*/")
                .NewLine();

            builder
                .AppendLine("import { Injectable } from '@angular/core';")
                .AppendLine("import { HttpClient } from '@angular/common/http';")
                .AppendLine("import { Observable } from 'rxjs';");

            var toImport = GetImportedTypes(file);
            toImport.IntersectWith(definedTypes);

            EmitImports(builder, file, files, toImport);
            EmitApiTypes(builder, file);

            builder.NewLine();
        }

        private void GenerateModel(CodeBuilder builder, FileDefinition fileToEmit)
        {
            builder
                .AppendLine("/*")
                .AppendLine("    GENERATED FILE")
                .AppendLine("    Any changes you make to this file by hand are going to be overwritten by MK94.CodeGenerator.Generator and the code generator there.")
                .AppendLine("*/")
                .NewLine();

            var toImport = GetImportedTypes(fileToEmit);
            toImport.IntersectWith(definedTypes);

            EmitImports(builder, fileToEmit, files, toImport);
            EmitEnums(builder, fileToEmit);
            EmitDataTypes(builder, fileToEmit);

            builder.NewLine();
        }

        private static HashSet<Type> GetImportableTypes(FileDefinition file)
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

        private static HashSet<Type> ExpandGenericType(Type type)
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

            return new []{ t };
        }

        private static void EmitImports(CodeBuilder builder, FileDefinition fileToEmit, List<FileDefinition> files, HashSet<Type> allTypes)
        {
            var imports = allTypes
                .GroupBy(x => GetImportPath(fileToEmit.Name, x, files))
                .Where(x => x.Key != null)
                .ToList();

            foreach (var import in imports)
            {
                var importsText = import.Select(i => GetTypeText(i, TypeText.Import)).Distinct().Aggregate((x, y) => $"{x}, {y}");

                builder.AppendLine($"import {{ {importsText} }} from '{import.Key}';");
            }

            builder.NewLine();
        }

        private static void EmitEnums(CodeBuilder builder, FileDefinition fileToEmit)
        {
            foreach (var e in fileToEmit.EnumTypes)
            {
                builder.Append($"export enum {e.Type.Name}");
                builder.OpenBlock();

                foreach (var kv in e.KeyValuePairs)
                {
                    builder.AppendLine($"{kv.Key} = {kv.Value},");
                }

                builder.CloseBlock();
            }
        }

        private static void EmitDataTypes(CodeBuilder builder, FileDefinition fileToEmit)
        {
            foreach (var d in fileToEmit.Types)
            {
                if(d.Type.GetCustomAttribute<IdFeature>() != null)
                {
                    builder.AppendLine($"export type {GetTypeText(d.Type, TypeText.Extension)} = string;");
                    continue;
                }

                if(d.Type.BaseType != null && d.Type.BaseType.IsGenericType && d.Type.BaseType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    builder.AppendLine($"export type {GetTypeText(d.Type, TypeText.Extension)} = {GetTypeExtensionsAsText(d.Type, false)};");
                    continue;
                }

                builder.Append($"export interface {GetTypeText(d.Type, TypeText.Extension)}{GetTypeExtensionsAsText(d.Type)}");
                builder.OpenBlock();

                foreach (var prop in d.Properties)
                {
                    var type = GetTypeText(prop.Type);

                    builder.AppendLine($"{prop.Name.ToCamelCase()}: {type} | null | undefined;");
                }

                builder.CloseBlock();
            }
        }

        private static void EmitApiTypes(CodeBuilder builder, FileDefinition fileToEmit)
        {
            foreach (var api in fileToEmit.Types)
            {
                var apiName = GetApiName(api.Type);

                builder
                    .AppendLine("@Injectable({")
                    .AppendLine("    providedIn: 'root',")
                    .AppendLine("})")
                    .Append($"export class {apiName.ToPascalCase()}Api")
                    .OpenBlock()
                    .AppendLine("constructor(private http: HttpClient) { }");

                foreach (var method in api.Methods)
                    EmitMethod(builder, method);

                builder.CloseBlock();
            }
        }

        public static string GetApiName(Type type) => type.Name.Replace("Controller", string.Empty).ToLower().TrimStart('i');

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

        private static void EmitMethod(CodeBuilder builder, MethodDefinition method)
        {
            var parameters = method.Parameters();

            builder
                .NewLine()
                .Append($"{method.Name.ToCamelCase()}")
                .OpenParanthesis();

            foreach (var prop in method.Parameters)
                builder
                    .Append($"{prop.Name}: {GetTypeText(prop.Type)}")
                    .AppendOptionalComma();

            builder
                .CloseParanthesis()
                .Append($" : Observable<{GetTypeText(method.ResponseType)}>")
                .OpenBlock();

            AppendApiUri(builder, method);
            AppendCallBody(builder, method, parameters);
            var hasParams = AppendCallParams(builder, method, parameters);

            builder
                .NewLine()
                .Append($"return this.http.{(method.IsGetRequest() ? "get" : "post")}");

            if (method.ResponseType != typeof(void) && method.ResponseType != typeof(Task))
                builder.Append($"<{GetTypeText(method.ResponseType)}>");

            builder.OpenParanthesis()
                .Append($"_url")
                .AppendOptionalComma();

            if (parameters.BodyParameters.Any() || parameters.FormParameters.Any())
                builder
                    .Append("_body")
                    .AppendOptionalComma();

            else if (hasParams && !method.IsGetRequest())
                builder
                    .Append("null")
                    .AppendOptionalComma();

            if (hasParams)
                builder.Append("_params");

            builder
                .CloseParanthesis()
                .Append(";")
                .CloseBlock();
        }

        private static bool AppendCallParams(CodeBuilder builder, MethodDefinition method, Parameters paramaters)
        {
            if (!method.IsAnonymous() && !paramaters.QueryParameters.Any())
                return false;

            builder
                .Append($"const _params =")
                .OpenBlock();

            if (method.IsAnonymous())
                builder
                    .AppendLine("withCredentials: false")
                    .AppendOptionalComma();

            if (paramaters.QueryParameters.Any())
            {
                builder
                    .Append("params: ")
                    .OpenBlock();

                foreach (var p in paramaters.QueryParameters)
                {
                    builder
                        .AppendLine(p)
                        .AppendOptionalComma();
                }

                builder.CloseBlock();
            }

            builder.CloseBlock();

            return true;
        }

        private static void AppendCallBody(CodeBuilder builder, MethodDefinition method, Parameters parameters)
        {
            if (parameters.FormParameters.Any())
            {
                builder.AppendLine("const _body = new FormData();");

                foreach (var formValue in parameters.FormParameters)
                    builder
                        .AppendLine($@"_body.append(""{formValue}"", {formValue});");

                builder.NewLine();
            }
            else if (parameters.BodyParameters.Any())
            {
                builder.AppendLine($"const _body = {parameters.BodyParameters.Single()};");
            }
        }

        private static void AppendApiUri(CodeBuilder builder, MethodDefinition method)
        {
            var route = $"`{method.Route()}`";

            foreach (var replacement in method.RouteReplacements())
                route = route.Replace($"{{{replacement}}}", $"${{{replacement}}}");

            builder.AppendLine($"const _url = {route};");
        }

        #region Text Helpers
        private static string GetTypeExtensionsAsText(Type type, bool includeExtendsKeyword = true)
        {
            var extensions = GetTypeExtensions(type).Select(x => GetTypeText(x, TypeText.Extension));

            if (!extensions.Any())
                return string.Empty;

            var aggregated = extensions.Aggregate((a, b) => $"{a}, {b}");

            if (includeExtendsKeyword)
                return $" extends {aggregated}";

            return aggregated;
        }

        enum TypeText
        {
            Generic,
            Import,
            Extension
        }

        private static string GetTypeText(Type type, TypeText mode = TypeText.Generic)
        {
            if (type == typeof(bool))
                return "boolean";
            else if (type == typeof(int))
                return "number";
            else if (type == typeof(decimal))
                return "number";
            else if (type == typeof(byte))
                return "number";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(Guid))
                return "string";
            else if (type == typeof(TimeSpan))
                return "string";
            else if (type == typeof(DateTime))
                return "Date";
            else if (type.Name == "IFileData")
                return "File";
            else if (type.Name == "IFormFile")
                return "File";
            else if (type == typeof(void) || type == typeof(Task))
                return "unknown";
            else if (type == typeof(System.IO.Stream))
                return "Blob";
            else if (type == typeof(System.Numerics.Vector3))
                return "unknown";
            else if (type == typeof(System.Numerics.Quaternion))
                return "Quaternion";
            else if (type == typeof(byte[]))
                return "ArrayBuffer";
            else if (type == typeof(IFileResult))
                return "ArrayBuffer";

            else if (type.IsArray)
                return GetTypeText(type.GetElementType()!) + "[]";

            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Stack<>))
                    return GetTypeText(type.GetGenericArguments()[0]) + "[]";

                if (type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>) || type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return GetTypeText(type.GetGenericArguments()[0], mode);

                if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                    return $"{GetTypeText(type.GetGenericArguments()[0], mode)}[]";

                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return $"{{ [key: string]: {GetTypeText(type.GenericTypeArguments[1])} | null }}";

                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var typeText = GetTypeText(type.GetGenericArguments()[0], mode);

                    if (mode == TypeText.Generic)
                        typeText += "[]";
                    else if (mode == TypeText.Extension)
                        typeText = $"Array<{typeText}>";

                    return typeText;
                }

                if (mode == TypeText.Import)
                    return type.Name.Remove(type.Name.IndexOf('`'));

                return type.Name.Remove(type.Name.IndexOf('`'))
                + "<"
                + type.GetGenericArguments().Select(x => GetTypeText(x, mode)).Aggregate((x, y) => $"{x}, {y}")
                + ">";
            }
            else
            {
                return type.Name;
            }
        }

        private static bool RequiresImport(Type type)
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
            if (type == typeof(System.Numerics.Vector3)) return false;
            if (type == typeof(System.Numerics.Quaternion)) return false;
            if (type == typeof(object)) return false;
            if (type == typeof(byte[])) return false;

            return true;
        }

        private static string? GetImportPath(string path, Type import, List<FileDefinition> files)
        {
            var importFileTarget = FindTypeInFiles(import, files);

            if (importFileTarget == null)
            {
                if (systemTypes.TryGetValue(import, out var importLocation))
                    return importLocation;

                return null;
            }

            var relative = Path.GetRelativePath(path, importFileTarget.Name);

            if (relative == ".") // same file
                return null;

            relative = relative.Replace(".ts", "");
            relative = relative.Replace("\\", "/");
            relative = relative.Substring(3, relative.Length - 3);
            return $"./{relative}";
        }

        private static FileDefinition? FindTypeInFiles(Type type, List<FileDefinition> files)
        {
            var matches = files.Where(f => f.Types.Any(d => d.Type == type) || f.EnumTypes.Any(e => e.Type == type));

            if (matches.Count() == 0)
                return null;

            if (matches.Count() > 1)
                throw new InvalidProgramException($"Type {type.FullName} exists multiple times in project");

            return matches.Single();
        }

        #endregion
    }
}