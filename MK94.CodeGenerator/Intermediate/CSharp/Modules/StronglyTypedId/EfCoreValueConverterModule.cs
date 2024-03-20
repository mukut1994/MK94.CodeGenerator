using System.Reflection;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

public class EfCoreValueConverterModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public EfCoreValueConverterModule(ICSharpProject project)
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

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                var converterClass = ns
                    .Type($"{typeDef.Type.Name}EfCoreValueConverter", MemberFlags.Public | MemberFlags.Partial)
                    .WithInheritsFrom(CsharpTypeReference.ToRaw($"global::Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<{typeDef.Type.Name}, global::System.Guid>"));

                converterClass
                    .Constructor(MemberFlags.Public)
                    .WithBaseConstructorCall($"id => id.Id, value => new {typeDef.Type.Name}(value), mappingHints")
                    .WithArgument(CsharpTypeReference.ToRaw("global::Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints?"), "mappingHints")
                    .DefaultValue("null");
            }
        }
    }
}