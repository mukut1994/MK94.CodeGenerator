using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class JsonConverterModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public JsonConverterModule(ICSharpProject project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        foreach(var fileDef in project.Files)
        {
            var file = codeGenerator.File($"{fileDef.Name}.g.cs");

            foreach(var typeDef in fileDef.Types)
            {
                if (typeDef.Properties.Count == 0)
                    continue;

                var propertiesWithJsonConverterAttribute = typeDef.Properties.Where(x => x.Info.GetCustomAttributes<JsonConverterAttribute>().Any()).ToList();

                if (propertiesWithJsonConverterAttribute.Count == 0)
                    continue;

                file.WithUsing("System.Text.Json");
                file.WithUsing("System.Text.Json.Serialization");

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                foreach(var property in propertiesWithJsonConverterAttribute)
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

public static class JsonConverterModuleExtensions
{
    public static T WithJsonConverterGenerator<T>(this T project, Action<JsonConverterModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new JsonConverterModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class JsonConverterAttribute : Attribute
{
}