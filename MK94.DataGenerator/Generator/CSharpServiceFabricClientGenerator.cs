using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MK94.DataGenerator.Generator.Generators.CSharpHelper;

namespace MK94.DataGenerator.Generator
{
    public class CSharpServiceFabricClientGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files, params string[] additionalNamespaces)
        {
            foreach(var file in files)
            {
                var output = builderFactory(file.Name + ".cs");
                Generate(output, @namespace, file, additionalNamespaces);
                output.Flush();
            }
        }

        public void Generate(CodeBuilder builder, string @namespace, FileDefinition fileDefinition, params string[] additionalNamespaces)
        {
            builder.AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Linq;")
                .AppendLine("using System.Text;")
                .AppendLine("using System.Threading.Tasks;")
                .AppendLine("using System.Numerics;");

            foreach (var n in additionalNamespaces)
                builder.AppendLine($"using {n};");

            builder
                .NewLine();

            builder
                .AppendLine($"namespace {@namespace}")
                .OpenBlock()
                .Append(Generate, fileDefinition)
                .CloseBlock();

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
                   .Append(b => GenerateInterface(b, type, type.Methods))
                   .Append(b => GenerateImplementation(b, type, type.Methods));
        }

        private void GenerateImplementation(CodeBuilder builder, TypeDefinition type, List<MethodDefinition> method)
        {
            builder
                   .AppendLine($"[System.Diagnostics.DebuggerStepThrough]")
                   .AppendLine($"public class {CSharpName(type.Type).Substring(1)} : {CSharpName(type.Type)}")
                   .OpenBlock()
                   .AppendLine($"private readonly ServiceFabric sv;")
                   .AppendLine($"public {CSharpName(type.Type).Substring(1)}(ServiceFabric sv) {{ this.sv = sv; }}")
                   .NewLine()
                   .Append((b, x) => GenerateImplementationMethod(b, type, x), method)
                   .CloseBlock();

        }

        private void GenerateImplementationMethod(CodeBuilder builder, TypeDefinition type, MethodDefinition method)
        {
            builder
                       .Append($"public {CSharpName(method.ResponseType)} {method.Name}")
                       .WithParenthesis(Generate, method.Parameters)
                       .OpenBlock();

            if (method.ResponseType == typeof(void) || method.ResponseType == typeof(Task))
            {
                builder
                           .Append($"return sv.Execute<{CSharpName(type.Type)}>")
                           .WithParenthesis((b, x) => b.Append(x).AppendOptionalComma(), new[] { @$"""{method.Name}""" }.Concat(method.Parameters.Select(x => x.Name)))
                           .AppendLine($"; ");
            }
            else
            {
                builder
                           .Append($"return sv.Execute<{CSharpName(type.Type)}, {CSharpName(UnwrapTask(method.ResponseType))}>")
                           .WithParenthesis((b, x) => b.Append(x).AppendOptionalComma(), new[] { @$"""{method.Name}""" }.Concat(method.Parameters.Select(x => x.Name)))
                           .AppendLine($"; ");
            }

            builder.CloseBlock();
        }

        private void GenerateInterface(CodeBuilder builder, TypeDefinition type, List<MethodDefinition> method)
        {
            builder
                .AppendLine($"public interface {CSharpName(type.Type)}{Extensions(type)}")
                .WithBlock((b, x) => GenerateInterfaceMethod(b, type, x), method);
        }

        private void GenerateInterfaceMethod(CodeBuilder builder, TypeDefinition type, MethodDefinition method)
        {
            builder
                .Append($"{CSharpName(method.ResponseType)} {method.Name}")
                .WithParenthesis(Generate, method.Parameters)
                .AppendLine(";");
        }

        private void Generate(CodeBuilder builder, ParameterDefinition def)
        {
            builder
                .Append($"{CSharpName(def.Type)} {def.Name}")
                .AppendOptionalComma();
        }

        private string Extensions(TypeDefinition type)
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
    }
}
