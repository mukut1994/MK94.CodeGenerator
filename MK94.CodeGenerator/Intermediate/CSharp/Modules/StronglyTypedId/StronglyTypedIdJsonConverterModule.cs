using System.Linq;
using System.Reflection;

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
                if (typeDef.Properties.Count == 0)
                    continue;

                var propertiesWithJsonConverterAttribute = typeDef
                    .Properties
                    .Where(x => x.Info.GetCustomAttributes<StronglyTypedIdAttribute>().Any(y => y.IncludeJsonConverter))
                    .ToList();

                if (propertiesWithJsonConverterAttribute.Count == 0)
                    continue;

                file.WithUsing("System.Text.Json");
                file.WithUsing("System.Text.Json.Serialization");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                foreach (var property in propertiesWithJsonConverterAttribute)
                {
                    var converterClass = ns
                        .Type($"{property.Name}Converter", MemberFlags.Public)
                        .WithInheritsFrom(CsharpTypeReference.ToRaw($"JsonConverter<{property.Name}>"));

                    converterClass
                        .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToRaw(property.Name), "Read")
                        .WithArgument(CsharpTypeReference.ToRaw("Utf8JsonReader"), "reader")
                        .WithArgument(CsharpTypeReference.ToRaw("Type"), "typeToConvert")
                        .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options")
                        .Body
                        .Append($"return new {property.Name}(Guid.Parse(reader.GetString()!));");

                    converterClass
                        .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToVoid(), "Write")
                        .WithArgument(CsharpTypeReference.ToRaw("Utf8JsonWriter"), "writer")
                        .WithArgument(CsharpTypeReference.ToRaw(property.Name), "value")
                        .WithArgument(CsharpTypeReference.ToRaw("JsonSerializerOptions"), "options")
                        .Body
                        .Append("writer.WriteStringValue(value.Id);");
                }
            }
        }
    }
}