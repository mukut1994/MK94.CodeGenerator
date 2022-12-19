using System;
using System.Collections.Generic;
using System.Linq;
using static MK94.CodeGenerator.Generator.Generators.CSharpHelper;

namespace MK94.CodeGenerator.Generator.Generators
{
    public class CSharpQueueConsumerGeneratorV2
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
                .AppendLine($"namespace {@namespace};")
                .AppendLine($"public static class QueueDefinitons")
                .WithBlock(Generate, consumers);
        }

        private void Generate(CodeBuilder builder, List<ConsumerInfo> consumers)
        {
            builder
                .Append(GenerateAll, consumers)
                .Append(GenerateGetter, consumers);
        }

        private void GenerateGetter(CodeBuilder builder, ConsumerInfo consumer)
        {
            var name = CSharpName(consumer.TypeDefinition.Type);

            builder
                .AppendLine($"public static QueueConfiguration {name}_{consumer.Method.Name} {{ get; }} = new()")
                .OpenBlock()
                    .AppendLine($@"Exchange = ""{consumer.Exchange}"",")
                    .AppendLine($@"Name = ""{consumer.Queue.Name}"",")
                    .AppendLine($@"RoutingKey = ""{consumer.Queue.RoutingKey}"",")
                    .AppendLine($@"Type = typeof({name}),")
                    .AppendLine($@"HandlerMethod = ""{consumer.Method.Name}"",")
                .CloseBlock()
                .AppendLine(";");
        }

        private void GenerateAll(CodeBuilder builder, List<ConsumerInfo> consumers)
        {
            builder
                .AppendLine($"public static IEnumerable<QueueConfiguration> All => new[]")
                .OpenBlock()
                    .Append((x, consumer) => x.AppendLine($"{CSharpName(consumer.TypeDefinition.Type)}_{consumer.Method.Name},"), consumers)
                .CloseBlock()
                .AppendLine(";");
        }
    }
}
