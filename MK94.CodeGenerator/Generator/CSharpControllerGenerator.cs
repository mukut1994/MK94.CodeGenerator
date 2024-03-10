﻿using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator
{
    public class CSharpControllerGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (file.Types.All(t => !t.Methods.Any()))
                    return;

                var output = builderFactory(file.Name + ".g.cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        private void Generate(CodeBuilder builder, string @namespace, FileDefinition file)
        {
            builder
                .AppendLine("// <auto-generated/>")
                .AppendLine($"using System; ")
                .AppendLine($"using System.Threading.Tasks; ")
                .AppendLine($"using System.Collections.Generic; ")
                .AppendLine($"using Microsoft.AspNetCore.Mvc; ")
                .AppendLine($"namespace {@namespace}")
                .WithBlock(Generate, file.Types);
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine(@"[Route(""api/{Controller}/{Action}"")]")
                .AppendLine($"public partial class {type.Type.Name.Substring(1, type.Type.Name.Length - 1)}")
                .WithBlock(Generate, type.Methods);
        }

        private void Generate(CodeBuilder builder, MethodDefinition method)
        {
            builder
                .AppendLine(method.IsGetRequest() ? @"[HttpGet]" : @"[HttpPost]")
                .Append($"public partial {GetTypeText(method.ResponseType)} {method.Name}")
                .WithParenthesis(Generate, method.Parameters)
                .AppendLine(";");
        }

        private void Generate(CodeBuilder builder, ParameterDefinition paramDef)
        {
            if (paramDef.Parameter.GetCustomAttributes<BodyAttribute>().Any())
                builder.Append($"[FromBody] ");
            else if (paramDef.Parameter.GetCustomAttributes<FormAttribute>().Any())
                builder.Append($"[FromForm] ");

            builder
                .Append($"{GetTypeText(paramDef.Type)} {paramDef.Name}")
                .AppendOptionalComma();
        }

        public static string GetTypeText(Type type)
        {
            if (type == typeof(void))
                return "void";
            if (type == typeof(bool))
                return "bool";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(IFormFile))
                return "Microsoft.AspNetCore.Http.IFormFile";
            else if (type == typeof(IFileResult))
                return "IActionResult";

            else if (type.IsGenericType)
            {
                return type.Name.Remove(type.Name.IndexOf('`'))
                + "<"
                + type.GetGenericArguments().Select(t => GetTypeText(t)).Aggregate((x, y) => $"{x}, {y}")
                + ">";
            }
            else
            {
                return type.Name;
            }
        }
    }
}
