using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

public class StronglyTypedIdModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public StronglyTypedIdModule(ICSharpProject project)
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

                var propertiesWithStronglyTypedAttribute = typeDef.Properties.Where(x => x.Info.GetCustomAttributes<StronglyTypedIdAttribute>().Any()).ToList();

                if (propertiesWithStronglyTypedAttribute.Count == 0)
                    continue;

                var ns = file.Namespace(project.NamespaceResolver(typeDef));
                CreateStronglyTypedIdInterface(ns, propertiesWithStronglyTypedAttribute);

                foreach (var property in propertiesWithStronglyTypedAttribute)
                {
                    switch (property.Type)
                    {
                        case Type guidType when guidType == typeof(Guid):
                            CreateGuidId(ns, property);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }

    private static void CreateGuidId(IntermediateFileDefinition.IntermediateNamespaceDefintion ns, PropertyDefinition property)
    {
        var stronglyTypedId = ns
                                .Type(property.Name, MemberFlags.Public)
                                .WithTypeAsRecord()
                                .WithTypeAsStruct()
                                .WithInheritsFrom(CsharpTypeReference.ToRaw($"{property.Type.Name}Id"))
                                .WithPrimaryConstructor();

        if (property.Info.GetCustomAttribute<StronglyTypedIdAttribute>()!.IncludeJsonConverter)
        {
            stronglyTypedId.Attribute(CsharpTypeReference.ToType<JsonConverterAttribute>()).WithParam($"typeof({property.Name}Converter)");
        }

        stronglyTypedId.Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToType<Guid>(), "Empty")
            .Body.Append("return new(Guid.Empty);");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToType<Guid>(), "New")
            .Body.Append("return new(Guid.NewGuid());");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString")
            .Body.Append("return Id.ToString();");
    }

    private static void CreateStronglyTypedIdInterface(IntermediateFileDefinition.IntermediateNamespaceDefintion ns, List<PropertyDefinition> propertiesWithStronglyTypedAttribute)
    {
        var uniquePropertyTypes = propertiesWithStronglyTypedAttribute.Select(x => x.Type).Distinct().ToList();

        foreach (var uniquePropertyType in uniquePropertyTypes)
        {
            var type = ns
                .Type($"{uniquePropertyType.Name}Id", MemberFlags.Public)
                .WithTypeAsInterface();

            type
                .Property(MemberFlags.Public, CsharpTypeReference.ToType(uniquePropertyType), "Id")
                .WithGetter();
        }
    }
}

public static class StronglyTypedIdModuleExtensions
{
    public static T WithStronglyTypedIdGenerator<T>(this T project, Action<StronglyTypedIdModule>? configure = null)
        where T : ICSharpProject
    {
        var mod = new StronglyTypedIdModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }

    public static T WithJsonConverterGenerator<T>(this T project, Action<StronglyTypedIdJsonConverterModule>? configure = null)
        where T : ICSharpProject
    {
        if (!project.GeneratorModules.Any(x => x.GetType() == typeof(StronglyTypedIdModule)))
            throw new InvalidProgramException("Cannot add JsonConverterGenerator when StronglyTypedIdGenerator is not added");

        var mod = new StronglyTypedIdJsonConverterModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }

    public static T WithEfCoreValueConverterGenerator<T>(this T project, Action<EfCoreValueConverterModule>? configure = null)
        where T : ICSharpProject
    {
        if (!project.GeneratorModules.Any(x => x.GetType() == typeof(StronglyTypedIdModule)))
            throw new InvalidProgramException("Cannot add EfCoreValueConverter when StronglyTypedIdGenerator is not added");

        var mod = new EfCoreValueConverterModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class StronglyTypedIdAttribute : Attribute
{
    public bool IncludeJsonConverter { get; set; }

    public StronglyTypedIdAttribute(bool includeJsonConverter = false)
    {
        IncludeJsonConverter = includeJsonConverter;
    }
}