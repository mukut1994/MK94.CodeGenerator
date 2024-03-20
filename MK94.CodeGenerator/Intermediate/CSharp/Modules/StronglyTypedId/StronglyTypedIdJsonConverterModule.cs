using System.Reflection;
using System.Text.Json.Serialization;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

public class StronglyTypedIdJsonConverterModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public StronglyTypedIdJsonConverterModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File($"{fileDef.Name}.g.cs");

            foreach (var typeDef in fileDef.Types)
            {
                if (typeDef.Type.GetCustomAttribute<StronglyTypedIdAttribute>() == null) continue;

                file.WithUsing("System.Text.Json");
                file.WithUsing("System.Text.Json.Serialization");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                var originalType = ns.Type(typeDef.Type.Name, MemberFlags.Public);

                originalType.Attribute(CsharpTypeReference.ToType<JsonConverterAttribute>()).WithParam($"typeof({originalType.Name}Converter)");

                var converterClass = ns
                    .Type($"{originalType.Name}Converter", MemberFlags.Public)
                    .WithInheritsFrom(CsharpTypeReference.ToRaw($"JsonConverter<{originalType.Name}>"));

                converterClass
                    .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToRaw(originalType.Name), "Read")
                    .WithArgument(CsharpTypeReference.ToRaw("Utf8JsonReader"), "reader")
                    .WithArgument(CsharpTypeReference.ToRaw("Type"), "typeToConvert")
                    .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options")
                    .Body
                    .Append($"return new {originalType.Name}(Guid.Parse(reader.GetString()!));");

                converterClass
                    .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToVoid(), "Write")
                    .WithArgument(CsharpTypeReference.ToRaw("Utf8JsonWriter"), "writer")
                    .WithArgument(CsharpTypeReference.ToRaw(originalType.Name), "value")
                    .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options")
                    .Body
                    .Append("writer.WriteStringValue(value.Id);");
            }
        }
    }
}