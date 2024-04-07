using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.Typescript.Modules;

public class TypeReflectionModule : IGeneratorModule<TypescriptCodeGenerator>
{
    private readonly ITypescriptProject project;

    public TypeReflectionModule(ITypescriptProject project)
    {
        this.project = project;
    }

    public void AddTo(TypescriptCodeGenerator codeGenerator)
    {
        foreach (var fileDef in project.Files)
        {
            var typeInfoType = codeGenerator.File($"{fileDef.Name}.ts")
                .Type("TypeInformation", MemberFlags.Type | MemberFlags.Interface);

            typeInfoType.WithGenericArgument("TKey");
            typeInfoType.Property(MemberFlags.Public, TsTypeReference.ToType<string>(), "type");
            typeInfoType.Property(MemberFlags.Public, TsTypeReference.ToNamed(null, "TKey"), "name");

            var infoConst = codeGenerator.File($"{fileDef.Name}.ts")
                .Constant("TypeInfomations", MemberFlags.Public | MemberFlags.Interface, TsTypeReference.ToAnonymous());

            foreach (var typeDef in fileDef.Types)
            {
                var typeInfoConst = codeGenerator.File($"{fileDef.Name}.ts")
                    .Constant($"_{typeDef.Type.Name}", MemberFlags.Const, TsTypeReference.ToNamed(null, $" TypeInformation<keyof {typeDef.Type.Name}>[] "))
                        .ArrayAsValue();

                foreach (var propDef in typeDef.Properties)
                {
                    typeInfoConst
                        .ObjectValue()
                            .WithStringProperty("type", TsTypeReference.CleanName(propDef.Info.PropertyType))
                            .WithStringProperty("name", propDef.Info.Name.ToCamelCase());
                }

                infoConst.ObjectAsValue().WithReferenceToConstantProperty(typeDef.Type.Name, $"_{typeDef.Type.Name}");
                infoConst.AfterMember($"_{typeDef.Type.Name}");
            }
        }
    }
}

public static class TypeReflectionModuleExtensions
{
    public static T WithTypeReflectionGenerator<T>(this T project, Action<TypeReflectionModule>? configure = null)
        where T : ITypescriptProject
    {
        var mod = new TypeReflectionModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}