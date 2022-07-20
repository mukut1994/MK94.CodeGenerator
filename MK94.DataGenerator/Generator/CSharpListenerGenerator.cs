using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Generator
{
    public class CSharpListenerGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (file.Types.All(t => !t.Methods.Any()))
                    continue;

                var output = builderFactory(file.Name + ".cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        public void Generate(CodeBuilder builder, string @namespace, FileDefinition fileDefinition)
        {
            builder.AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Linq;")
                .AppendLine("using System.Text;")
                .AppendLine("using System.IO;")
                .AppendLine("using System.Threading.Tasks;")
                .AppendLine("using System.Threading.Tasks.Dataflow; ")
                .NewLine();

            builder
                .AppendLine($"namespace {@namespace}")
                .OpenBlock()
                .Append(GenerateInterface, fileDefinition.Types)
                .Append(GenerateListener, fileDefinition.Types)
                .CloseBlock();

            builder.Flush();
        }

        private void GenerateInterface(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"public interface I{type.Type.Name}")
                .WithBlock(GenerateInterfaceMethods, type.Methods);

        }

        private void GenerateInterfaceMethods(CodeBuilder builder, MethodDefinition method)
        {
            builder
                .Append($"Task {method.Name}")
                .WithParenthesis(GenerateParameters, method.Parameters)
                .AppendLine(";");
        }

        private void GenerateParameters(CodeBuilder builder, ParameterDefinition parameter)
        {
            builder.Append($"{parameter.Type.Name} {parameter.Name}");
        }

        private void GenerateListener(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"public class {type.Type.Name}Listener")
                .WithBlock(x => x
                    .AppendLine("private BinaryReader reader;")
                    .AppendLine($"private I{type.Type.Name} i;")
                    .NewLine()
                    .AppendLine($"public {type.Type.Name}Listener(BinaryReader reader, I{type.Type.Name} i) {{ this.reader = reader; this.i = i; }}")
                    .NewLine()
                    .AppendLine(@"
public Task ProcessOne()
{
    var messageCode = reader.ReadByte();
    return ProcessMessage(messageCode);
}")
                    .Append(WriteProcessMessage, type)
                 );
        }

        private void WriteProcessMessage(CodeBuilder builder, TypeDefinition type)
        {
            void WriteCase(CodeBuilder builder, MethodDefinition method)
            {
                var messageCode = method.GetMessageCode();
                builder
                    .AppendLine($"case {messageCode}:")
                    .Append($"await i.{method.Name}")
                    .WithParenthesis((b, i) => b.Append($"reader.Read{i.Type.Name}()"), method.Parameters)
                    .AppendLine(";")
                    .AppendLine($"return;")
                    .NewLine();
            }

            builder
                .AppendLine("private async Task ProcessMessage(int type)")
                .WithBlock(x => x
                    .AppendLine("switch(type)")
                    .WithBlock(WriteCase, type.Methods)
                );
        }

        private void WriteProcessMessage_BufferBlocks(CodeBuilder builder, TypeDefinition type)
        {
            void WriteCase(CodeBuilder builder, MethodDefinition method)
            {
                var messageCode = method.GetMessageCode();
                builder.AppendLine($"case {messageCode}:");

                // Args read from stream
                foreach (var (p, i) in method.Parameters.Select((p, i) => (p, i)))
                    builder.AppendLine($"var arg{messageCode}_{i} = reader.Read{p.Type.Name}();");

                // call method
                builder
                    .AppendLine($"{method.Name}.Post(arg{messageCode}_0);");
                    /*
                    .OpenParanthesis();
                foreach (var (p, i) in method.Parameters.Select((p, i) => (p, i)))
                    builder.Append($"arg{messageCode}_{i}").AppendOptionalComma();

                builder.CloseParanthesis().AppendLine(";");*/

                builder.AppendLine($"return;").NewLine();
            }

            builder
                .AppendLine("private void ProcessMessage(int type)")
                .WithBlock(x => x
                    .AppendLine("switch(type)")
                    .WithBlock(WriteCase, type.Methods)
                );
        }
    }
}
