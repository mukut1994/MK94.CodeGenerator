using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator.Generators
{
    public class CSharpCopyToGenerator
    {
        private string toName;
        private string targetNamespace;
        private string sourceNamespace;

        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, string toName, string targetNamespace, List<FileDefinition> files)
        {
            this.toName = toName;
            this.targetNamespace = targetNamespace;
            this.sourceNamespace = @namespace;

            foreach (var file in files)
            {
                var output = builderFactory(file.Name + ".g.cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        private void Generate(CodeBuilder builder, string @namespace, FileDefinition file)
        {
            builder
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .NewLine()
                .AppendLine($"namespace {@namespace};")
                .NewLine()
                .AppendLine($"public static partial class CopyExtensions")
                .WithBlock(Generate, file);
        }

        private void Generate(CodeBuilder builder, FileDefinition file)
        {
            builder.Append(Generate, file.EnumTypes);
            builder.Append(Generate, file.Types);
        }

        private void Generate(CodeBuilder builder, EnumDefintion type)
        {
            var genericPart = type.Type  
                .GetGenericArguments()  
                .Select(x => x.Name)  
                .Aggregate((a, b) => $"{a}, {b}");  

            var generic = type.Type.IsGenericType ? $"<{genericPart}>" : "";
        
            var typeName = CSharpHelper.CSharpName(type.Type);
            var targetName = $"{targetNamespace}.{typeName}";
            var sourceName = $"{sourceNamespace}.{typeName}";
        
            // async value
            builder
                .AppendLine($"public static async Task<{targetName}> CopyTo{toName}{generic}Async(this Task<{sourceName}> value)")
                .OpenBlock()
                    .AppendLine($"return (await value).CopyTo{toName}();")
                .CloseBlock();
        
            // value
            builder
                .AppendLine($"public static {targetName} CopyTo{toName}{generic}(this {sourceName} value)")
                .OpenBlock()
                    .AppendLine($"return ({targetName})value;")
                .CloseBlock();
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"public static {targetNamespace}.{type.Type.Name} CopyTo{toName}(this {sourceNamespace}.{type.Type.Name} value)")
                .OpenBlock()
                    .AppendLine($"var ret = new {targetNamespace}.{type.Type.Name}();")
                    .Append((b, x) => Generate(b, x, type), type.Properties)
                    .AppendLine("return ret;")
                .CloseBlock();;
        }

        private void Generate(CodeBuilder builder, PropertyDefinition p, TypeDefinition type)
        {
            if(p.Type.IsPrimitive || p.Type.IsValueType || p.Type == typeof(string))
            {
                builder
                    .AppendLine($"ret.{p.Name} = value.{p.Name};");

                return;
            }

            if(p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var genDef = p.Type.GetGenericArguments()[0];

                builder
                    .AppendLine($"ret.{p.Name} = value.{p.Name}.CopyTo<{sourceNamespace}.{CSharpHelper.CSharpName(genDef)}, {targetNamespace}.{CSharpHelper.CSharpName(genDef)}>(CopyTo{toName});");

                return;
            }

            if (p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyDef = p.Type.GetGenericArguments()[0];
                var genDef = p.Type.GetGenericArguments()[1];

                builder
                    .AppendLine($"ret.{p.Name} = value.{p.Name}.CopyTo<{keyDef.FullName}, {sourceNamespace}.{CSharpHelper.CSharpName(genDef)}, {targetNamespace}.{CSharpHelper.CSharpName(genDef)}>(CopyTo{toName});");

                return;
            }

            builder
                .AppendLine($"ret.{p.Name} = value.{p.Name}.CopyTo{toName}();");
        }
    }
}
