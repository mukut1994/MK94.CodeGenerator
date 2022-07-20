using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MK94.DataGenerator.Generator.Generators.CSharpHelper;

namespace MK94.DataGenerator.Generator.Generators
{
    public class QueueBinding
    {
        public QueueBinding(string name, string routingKey, MethodInfo? handlerMethod)
        {
            Name = name;
            RoutingKey = routingKey;
            HandlerMethod = handlerMethod;
        }

        public string Name { get; init; }

        public string RoutingKey { get; set; }

        public MethodInfo? HandlerMethod { get; set; }
    }

    public class Exchange
    {
        public string Name { get; set; }

        public string RoutingKey { get; set; }

        public MethodInfo Publisher { get; set; }

        public List<QueueBinding> Queues { get; set; } = new();

        public Exchange(string name, string routingKey, MethodInfo publisher)
        {
            Name = name;
            Publisher = publisher;
            RoutingKey = routingKey;

            if (routingKey.Contains("{id}") && publisher.GetParameters().All(x => x.Name != "id"))
                throw new InvalidProgramException($"Exchange '{name}' requires a method parameter called 'id' on handler {publisher.Name}");
        }

        public Exchange WithQueue(string routingKey, MethodInfo info)
        {
            var name = $"{info.DeclaringType!.Name}.{info.Name}";

            var queue = new QueueBinding(name, routingKey, info);

            if (info.GetParameters().Length > 2 && info.GetParameters()[0].ParameterType != Publisher.GetParameters().First(x => x.Name != "id").ParameterType)
                throw new InvalidProgramException($"Exchange {Name} has different parameters to Queue {name}");

            Queues.Add(queue);

            return this;
        }
    }

    public class CSharpQueuePublisherGenerator
    {
        private record PublisherInfo(Exchange Exchange, TypeDefinition Type, MethodDefinition Method);

        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> publishers, List<string>? additionalNamespaces = null)
        {
            foreach (var file in publishers)
            {
                var outputTypes = PublisherTypes(file).GroupBy(x => x.Type);

                if (!outputTypes.Any())
                    continue;

                var output = builderFactory($"publisher_{file.Name}.cs");
                GeneratePublisher(output, @namespace, outputTypes, additionalNamespaces ?? new List<string>());
                output.Flush();
            }
        }

        private List<PublisherInfo> PublisherTypes(FileDefinition fileDefinition)
        {
            var ret = new List<PublisherInfo>();

            var exchangeLookup = ExchangeInfo.Exchanges
                .GroupBy(x => x.Publisher!)
                .ToDictionary(x => x.Key, x => x);

            foreach (var type in fileDefinition.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!exchangeLookup.TryGetValue(method.MethodInfo, out var publishers))
                        continue;

                    foreach (var publisher in publishers)
                    {
                        ret.Add(new(publisher, type, method));
                    }
                }
            }

            return ret;
        }

        private void GeneratePublisherInterface(CodeBuilder builder, IEnumerable<PublisherInfo> publisher)
        {
            var name = CSharpName(publisher.First().Type.Type);
            builder
                .AppendLine($"public interface {name}")
                .OpenBlock()
                    .Append(GeneratePublisherMethodStub, publisher)
                .CloseBlock();
        }

        private void GeneratePublisherMethodStub(CodeBuilder builder, PublisherInfo publisher)
        {
            builder
                .Append($"void {publisher.Method.Name}")
                .WithParenthesis(Generate, publisher.Method.Parameters)
                .AppendLine(";");
        }

        private void GeneratePublisher(CodeBuilder builder, string @namespace, IEnumerable<IGrouping<TypeDefinition, PublisherInfo>> types, List<string> additionalNamespaces)
        {
            builder
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Text.Json;")
                .AppendLine("using RabbitMQ.Client;")
                .Append((CodeBuilder b, string c) => b.AppendLine($"using {c};"), additionalNamespaces)
                .NewLine()
                .AppendLine($"namespace {@namespace}")
                .WithBlock(GeneratePublisher, types);
        }

        private void GeneratePublisher(CodeBuilder builder, IEnumerable<PublisherInfo> publisher)
        {
            // TODO not only for FTP? or remove
            var interfaceName = CSharpName(publisher.First().Type.Type);
            var name = interfaceName[1..];
            builder
                .Append(GeneratePublisherInterface, publisher)
                .AppendLine($"public class {name} : {interfaceName}")
                .OpenBlock()
                    .AppendLine($"private readonly IModel model;")
                    .AppendLine($"private readonly JsonSerializerOptions jsonOptions;")
                    .AppendLine($"public {name}(FTP.Infrastructure.IQueueChannelFactory factory, JsonSerializerOptions jsonOptions) {{ model = factory.CreateChannel(); this.jsonOptions = jsonOptions; }}")
                    .Append(GenerateBody, publisher)
                .CloseBlock();
        }

        private void GenerateBody(CodeBuilder builder, PublisherInfo publisher)
        {
            var exchange = publisher.Exchange.Name;
            var key = "$\"" + publisher.Exchange.RoutingKey + "\"";
            
            builder
                .Append($"public void {publisher.Method.Name}")
                .WithParenthesis(Generate, publisher.Method.Parameters)
                .OpenBlock()
                    .AppendLine($"var routingKey = {key};")
                    .AppendLine($"var body = JsonSerializer.SerializeToUtf8Bytes({publisher.Method.Parameters.First(x => x.Name != "id").Name}, jsonOptions);")
                    .AppendLine($@"model.BasicPublish(""{exchange}"", routingKey, null, body);")
                .CloseBlock();
        }

        private void Generate(CodeBuilder builder, ParameterDefinition def)
        {
            builder
                .Append($"{CSharpName(def.Type)} {def.Name}")
                .AppendOptionalComma();
        }
    }
}
