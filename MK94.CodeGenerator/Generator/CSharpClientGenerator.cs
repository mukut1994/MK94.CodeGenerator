using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator
{
    public class CSharpClientGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach(var file in files)
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
            builder
                .AppendUsings("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO", "System.Threading.Tasks")
                .AppendNamespace(@namespace)
                .WithBlock(Generate, fileDefinition);

            builder.Flush();
        }

        private void Generate(CodeBuilder builder, FileDefinition fileDefinition)
        {
            builder
                .Append(Generate, fileDefinition.Types);
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                   .AppendLine($"public class {type.Type.Name}")
                   .WithBlock(x => x
                       .AppendLine("private BinaryWriter writer;")
                       .NewLine()
                       .AppendLine($"public {type.Type.Name}(BinaryWriter writer) {{ this.writer = writer; }}")
                       .NewLine()
                       .Append(Generate, type.Methods)
                   );
        }

        private void Generate(CodeBuilder builder, MethodDefinition method)
        {
            builder
                .Append($"public void {method.Name}({method.Parameters[0].Type.Name} {method.Parameters[0].Name})")
                .WithBlock(x => x
                    .Append(WriteMessageBody, method)
                 );
        }

        private void WriteMessageBody(CodeBuilder builder, MethodDefinition method)
        {
            var p = method.Parameters[0];

            // builder.AppendLine($"writer.Write((byte){method.MethodInfo.GetCustomAttributesUngrouped<MessageCodeAttribute>().Single().Code});");

            if (p.Type == typeof(string))
            {
                builder.AppendLine($"writer.Write({p.Name}.AsSpan(0, {p.Name}.Length > short.MaxValue ? short.MaxValue : {p.Name}.Length).ToString()); ");
            }
            else
            {
                builder.AppendLine($"writer.Write({p.Name}); ");
            }

            builder.AppendLine($"writer.Flush();");
        }
    }
}
