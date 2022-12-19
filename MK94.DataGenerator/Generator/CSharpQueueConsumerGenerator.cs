using System;
using System.Collections.Generic;
using System.Linq;
using static MK94.CodeGenerator.Generator.Generators.CSharpHelper;

namespace MK94.CodeGenerator.Generator.Generators
{
    public class ExchangeInfo
    {
        // TODO build mermaid graph generator
        public static List<Exchange> Exchanges = new()
        {
        };
    }

    public class CSharpQueueConsumerGenerator
    {
        private record ConsumerInfo(Exchange Exchange, QueueBinding Queue, TypeDefinition TypeDefinition, MethodDefinition Method);

        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> consumers)
        {
            foreach (var file in consumers)
            {
                var outputTypes = ConsumerTypes(file);

                if (!outputTypes.Any())
                    continue;

                var output = builderFactory($"consumer_{file.Name}.cs");
                Generate(output, @namespace, outputTypes);
                output.Flush();
            }
        }

        private List<ConsumerInfo> ConsumerTypes(FileDefinition fileDefinition)
        {
            var ret = new List<ConsumerInfo>();

            var queueLookup = ExchangeInfo.Exchanges
                .SelectMany(x => x.Queues.Select(q => new { Exchange = x, Queue = q }))
                .GroupBy(x => x.Queue.HandlerMethod!)
                .ToDictionary(x => x.Key, x => x);

            foreach(var type in fileDefinition.Types)
            {
                foreach(var method in type.Methods)
                {
                    if (!queueLookup.TryGetValue(method.MethodInfo, out var handlers))
                        continue;

                    foreach (var handler in handlers)
                    {
                        ret.Add(new(handler.Exchange, handler.Queue, type, method));
                    }
                }
            }

            return ret;
        }

        private void Generate(CodeBuilder builder, string @namespace, List<ConsumerInfo> consumers)
        {
            builder
                .AppendLine("using System.Text.Json;")
                .AppendLine("using Microsoft.Extensions.DependencyInjection;")
                .AppendLine("using RabbitMQ.Client;")
                .NewLine()
                .AppendLine($"namespace {@namespace}")
                .WithBlock(Generate, consumers.GroupBy(x => x.TypeDefinition));
        }

        private void Generate(CodeBuilder builder, IGrouping<TypeDefinition, ConsumerInfo> consumer)
        {
            var name = CSharpName(consumer.Key.Type)[1..];

            builder
                .AppendLine($"public static class {name}ConsumerHelper")
                .OpenBlock()
                    .Append(GenerateMethod, consumer.GroupBy(x => x.Method))
                .CloseBlock();
        }

        private void GenerateMethod(CodeBuilder builder, IGrouping<MethodDefinition, ConsumerInfo> consumer)
        {
            builder
                .Append($"public static void BeginConsume{consumer.Key.Name}(IServiceProvider services)")
                .WithBlock(GenerateCall, consumer);
        }

        private void GenerateCall(CodeBuilder builder, ConsumerInfo consumer)
        {
            var method = consumer.Method;
            var queue = consumer.Queue;

            if (queue == null)
                return;

            var retType = CSharpName(method.MethodInfo.GetParameters().First(x => x.Name != "id").ParameterType);
            var typeName = CSharpName(consumer.Method.MethodInfo.DeclaringType!);

            builder
                .Append($"QueueHelper.SetupConsumer<{typeName}, {retType}>")
                .OpenParanthesis()
                    .Append("services").AppendOptionalComma()
                    .Append($"services.GetRequiredService<{typeName}>()").AppendOptionalComma()
                    .Append($"(s, arg) => s.{method.Name}(arg)").AppendOptionalComma()
                    .Append($@"""{consumer.Exchange.Name}""").AppendOptionalComma()
                    .Append($@"""{queue.Name}""").AppendOptionalComma()
                    .Append($@"""{queue.RoutingKey}""").AppendOptionalComma()
                .CloseParanthesis()
                .AppendLine(";");
        }

        private void Generate(CodeBuilder builder, ParameterDefinition def)
        {
            builder
                .Append($"{CSharpName(def.Type)} {def.Name}")
                .AppendOptionalComma();
        }
    }
}
