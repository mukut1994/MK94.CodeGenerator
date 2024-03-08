﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Generator.Generators
{
    public class CSharpStrongTypeIdGenerator
    {
        public void Generate(Func<string, CodeBuilder> builderFactory, string @namespace, List<FileDefinition> files)
        {
            foreach (var file in files)
            {
                var output = builderFactory(file.Name + ".cs");
                Generate(output, @namespace, file);
                output.Flush();
            }
        }

        private void Generate(CodeBuilder builder, string @namespace, FileDefinition fileDefinition)
        {
            builder
                .AppendUsings("System", "System.Text.Json", "System.Text.Json.Serialization");

            if (!fileDefinition.FileScopedNamespace)
            {
                builder.WithBlockedNamespace(@namespace, Generate, fileDefinition.Types);
            }
            else
            {
                builder.WithFileScopedNamespace(@namespace, Generate, fileDefinition.Types);
            }
        }

        private void Generate(CodeBuilder builder, TypeDefinition type)
        {
            builder
                .AppendLine($"[JsonConverter(typeof(Converter))]")
                .AppendLine($"public record struct {type.Type.Name}(Guid Id) : IId")
                .OpenBlock()
                .Append(GenerateToStringOverride)
                .NewLine()
                .Append(GenerateConverter, type.Type.Name)
                .CloseBlock();
        }

        private void GenerateToStringOverride(CodeBuilder builder)
        {
            builder
                .AppendLine("public override string ToString()")
                .AppendLine("{")
                .AppendLine("   return Id.ToString();")
                .AppendLine("}");
        }

        private void GenerateConverter(CodeBuilder builder, string name)
        {
            builder
                .AppendLine($"public class Converter : JsonConverter<{name}>")
                .AppendLine("{")
                .AppendLine($"    public override {name} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)")
                .AppendLine("    {")
                .AppendLine($"        return new {name}(Guid.Parse(reader.GetString()!));")
                .AppendLine("    }")
                .NewLine()
                .AppendLine($"    public override void Write(Utf8JsonWriter writer, {name} value, JsonSerializerOptions options)")
                .AppendLine("    {")
                .AppendLine("        writer.WriteStringValue(value.Id);")
                .AppendLine("    }")
                .AppendLine("}");
        }
    }
}
