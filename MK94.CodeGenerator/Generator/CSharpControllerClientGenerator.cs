using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MK94.CodeGenerator.Generator.CSharpHelper;

namespace MK94.CodeGenerator.Generator
{
    public class CSharpControllerClientGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (file.Types.All(t => !t.Methods.Any()))
                    continue;

                var output = builderFactory(file.Name + ".g.cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        private void Generate(CodeBuilder builder, string @namespace, FileDefinition fileDefinition)
        {
            builder
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Linq;")
                .AppendLine("using System.Text;")
                .AppendLine("using System.IO;")
                .AppendLine("using Flurl;")
                .AppendLine("using Flurl.Http;")
                .AppendLine("using System.Threading.Tasks;")
                .NewLine();

            builder
                .AppendLine($"namespace {@namespace}")
                .WithBlock(Generate, fileDefinition.Types);
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"public class {CSharpName(type.Type)[1..]}")
                .OpenBlock()
                    .AppendLine($"private readonly FlurlClient client;")
                    .AppendLine($"public {CSharpName(type.Type)[1..]}(FlurlClient client) {{ this.client = client; }}")
                    .NewLine()
                    .Append(Generate, type.Methods)
                .CloseBlock();
        }

        private void Generate(CodeBuilder builder, MethodDefinition method)
        {
            var controllerName = method.MethodInfo.DeclaringType!.Name[1..^10];
            var isVoidReturn = method.ResponseType == typeof(void) || method.ResponseType == typeof(Task);

            if (isVoidReturn)
                builder.Append($"public async Task {method.Name}");
            else
                builder.Append($"public async Task<{CSharpName(UnwrapTask(method.ResponseType))}> {method.Name}");


            builder
                .WithParenthesis(Generate, method.Parameters)
                .OpenBlock()
                    .Append($"{(isVoidReturn ? "" : "return ")}await client.Request")
                    .OpenParanthesis()
                        .Append(@$"""/api/{controllerName}/{method.Name}""")
                        .Append(GenerateQueryParamList, method.Parameters)
                    .CloseParanthesis();

            if (method.IsGetRequest())
            {
                if(isVoidReturn)
                    builder.Append($".GetAsync()");
                else
                    builder.Append($".GetJsonAsync<{CSharpName(UnwrapTask(method.ResponseType))}>()");
            }
            else
            {
                // TODO add form support
                if(!method.Parameters.Any(x => x.FromForm()))
                    builder.Append($".PostJsonAsync({method.Parameters.Single(p => p.FromBody()).Name})");

                if (!isVoidReturn)
                    builder.Append($".ReceiveJson<{CSharpName(UnwrapTask(method.ResponseType))}>()");
            }


            builder
                .AppendLine(";")
                .CloseBlock()
                .NewLine();
        }

        private void GenerateQueryParamList(CodeBuilder builder, ParameterDefinition param)
        {
            if (!param.FromQuery())
                return;

            builder
                .Append($@".SetQueryParam(""{param.Name}"", {param.Name})");
        }

        private void Generate(CodeBuilder builder, ParameterDefinition par)
        {
            builder
                .Append($"{CSharpName(par.Type)} {par.Name}")
                .AppendOptionalComma();
        }
    }
}
