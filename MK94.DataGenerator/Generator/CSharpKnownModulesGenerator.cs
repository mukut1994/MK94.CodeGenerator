using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Generator
{
    public class CSharpKnownModulesGenerator
    {
        private static Dictionary<Type, object> instanceCache = new();

        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach (var file in files)
            {
                if (file.Types.All(t => !t.Properties.Any()))
                    continue;

                var output = builderFactory(file.Name + ".cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        private void Generate(CodeBuilder builder, string @namespace, FileDefinition file)
        {
            builder
                .AppendLine($"using System;")
                .NewLine()
                .AppendLine($"namespace {@namespace}")
                .WithBlock(Generate, file.Types);
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"public static class {type.Type.Name}")
                .WithBlock(Generate, type.Properties);
        }

        private void Generate(CodeBuilder builder, PropertyDefinition def)
        {
            if(!instanceCache.TryGetValue(def.Info.DeclaringType, out var instance))
                instance = instanceCache[def.Info.DeclaringType] = Activator.CreateInstance(def.Info.DeclaringType);

            var value = (Guid) def.Info.GetValue(instance);

            builder
                .AppendLine($@"public static Guid {def.Name} {{ get; }} = Guid.Parse(""{value}"");");
        }
    }
}
