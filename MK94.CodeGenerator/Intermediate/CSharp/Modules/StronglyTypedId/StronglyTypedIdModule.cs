using System;
using System.Linq;
using System.Reflection;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

[AttributeUsage(AttributeTargets.Struct)]
public class StronglyTypedIdAttribute(Type? type = null) : Attribute
{
    public Type Type { get; set; } = type ?? typeof(Guid);
}

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
                var attribute = typeDef.Type.GetCustomAttribute<StronglyTypedIdAttribute>();

                if (attribute is null) continue;

                var ns = file.Namespace(project.NamespaceResolver(typeDef));

                CreateStronglyTypedIdInterface(ns, attribute);

                switch (attribute.Type)
                {
                    case var guidType when guidType == typeof(Guid):
                        CreateGuidId(ns, typeDef.Type.Name, attribute.Type.Name);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    private static void CreateGuidId(IntermediateFileDefinition.IntermediateNamespaceDefinition ns, string typeName, string backingType)
    {
        var stronglyTypedId = ns
                                .Type(typeName, MemberFlags.Public)
                                .WithTypeAsRecord()
                                .WithTypeAsStruct()
                                .WithInheritsFrom(CsharpTypeReference.ToRaw($"{backingType}Id"))
                                .WithPrimaryConstructor();

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

    private static void CreateStronglyTypedIdInterface(IntermediateFileDefinition.IntermediateNamespaceDefinition ns, StronglyTypedIdAttribute attribute)
    {
        var type = ns
            .Type($"{attribute.Type.Name}Id", MemberFlags.Public)
            .WithTypeAsInterface();

        type
            .Property(MemberFlags.Public, CsharpTypeReference.ToType(attribute.Type), "Id")
            .WithGetter();
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

    public static T WithJsonConverterForStronglyTypedIdGenerator<T>(this T project, Action<StronglyTypedIdJsonConverterModule>? configure = null)
        where T : ICSharpProject
    {
        if (project.GeneratorModules.All(x => x.GetType() != typeof(StronglyTypedIdModule)))
            throw new InvalidProgramException("Cannot add JsonConverterGenerator when StronglyTypedIdGenerator is not added");

        var mod = new StronglyTypedIdJsonConverterModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }

    public static T WithEfCoreValueConverterForStronglyTypedIdGenerator<T>(this T project, Action<EfCoreValueConverterModule>? configure = null)
        where T : ICSharpProject
    {
        if (project.GeneratorModules.All(x => x.GetType() != typeof(StronglyTypedIdModule)))
            throw new InvalidProgramException("Cannot add EfCoreValueConverter when StronglyTypedIdGenerator is not added");

        var mod = new EfCoreValueConverterModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }
}