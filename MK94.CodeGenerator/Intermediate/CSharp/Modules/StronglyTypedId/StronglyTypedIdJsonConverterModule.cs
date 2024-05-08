using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

public class StronglyTypedIdJsonConverterModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public StronglyTypedIdJsonConverterModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

            foreach (var typeDef in fileDef.Types)
            {
                var attribute = typeDef.Type.GetCustomAttribute<StronglyTypedIdAttribute>();
                if (attribute == null) continue;

                file.WithUsing("System.Text.Json");
                file.WithUsing("System.Text.Json.Serialization");

                var ns = file.Namespace(typeDef.GetNamespace());

                var originalType = ns.Type(typeDef.Type.Name, MemberFlags.Public, CsharpTypeReference.ToRaw(typeDef.Type.Name));

                originalType.Attribute(CsharpTypeReference.ToType<JsonConverterAttribute>()).WithParam($"typeof({originalType.Name}Converter)");

                var converterClass = ns
                    .Type($"{originalType.Name}Converter", MemberFlags.Public, CsharpTypeReference.ToRaw($"{originalType.Name}Converter"))
                    .WithInheritsFrom(CsharpTypeReference.ToRaw($"JsonConverter<{originalType.Name}>"));

                var method = converterClass
                    .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToRaw(originalType.Name), "Read")
                    // TODO "ref Utf8JsonReader" is a hack
                    .WithArgument(CsharpTypeReference.ToRaw("ref Utf8JsonReader"), "reader")
                    .WithArgument(CsharpTypeReference.ToRaw("Type"), "typeToConvert")
                    .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options");

                if(attribute.Type == typeof(Guid))
                    method.Body.Append($"return new {originalType.Name}(Guid.Parse(reader.GetString()!));");
                else
                    method.Body.Append($"return new {originalType.Name}(reader.GetString()!);");

                converterClass
                    .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToVoid(), "Write")
                    .WithArgument(CsharpTypeReference.ToRaw("Utf8JsonWriter"), "writer")
                    .WithArgument(CsharpTypeReference.ToRaw(originalType.Name), "value")
                    .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options")
                    .Body.Append("writer.WriteStringValue(value.Id);");
            }
        }
    }
}