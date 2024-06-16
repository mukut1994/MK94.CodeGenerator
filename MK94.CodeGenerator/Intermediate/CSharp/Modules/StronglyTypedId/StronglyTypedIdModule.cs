using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;

[AttributeUsage(AttributeTargets.Struct)]
public class StronglyTypedIdAttribute(Type? type = null) : FeatureAttribute
{
    public Type Type { get; set; } = type ?? typeof(Guid);
}

public class StronglyTypedIdModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly IFeatureGroup<CSharpCodeGenerator> project;

    public StronglyTypedIdModule(IFeatureGroup<CSharpCodeGenerator> project)
    {
        this.project = project;
    }

    public void AddTo(CSharpCodeGenerator codeGenerator)
    {
        var createdInterfaceType = new HashSet<Type>();

        foreach (var fileDef in project.Files)
        {
            var file = codeGenerator.File(fileDef.GetFilename() + ".cs");

            foreach (var typeDef in fileDef.Types)
            {
                var attribute = typeDef.Type.GetCustomAttribute<StronglyTypedIdAttribute>();

                if (attribute is null) continue;

                var ns = file.Namespace(typeDef.GetNamespace());

                // TODO figure out a better way 
                // this creates the interface in the first file
                // it needs to be potentially created in multiple namespaces
                // and ideally it would also support creating the interface in a generic namespace but the ids in more specific ones
                if (!createdInterfaceType.Contains(attribute.Type))
                {
                    CreateStronglyTypedIdInterface(ns, attribute);
                    createdInterfaceType.Add(attribute.Type);
                }

                switch (attribute.Type)
                {
                    case var guidType when guidType == typeof(Guid):
                        CreateGuidId(ns, typeDef.Type.Name, attribute.Type.Name, CsharpTypeReference.ToType(typeDef.Type));
                        break;

                    case var intType when intType == typeof(int):
                        CreateId(ns, typeDef, attribute.Type.Name, "new(0)", "new(Random.Shared.Next())", "Id.ToString()");
                        break;

                    case var stringType when stringType == typeof(string):
                        CreateStringId(ns, typeDef.Type.Name, attribute.Type.Name, CsharpTypeReference.ToType(typeDef.Type));
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    private static void CreateGuidId(IntermediateNamespaceDefintion ns, string typeName, string backingType, CsharpTypeReference type)
    {
        var stronglyTypedId = ns
                                .Type(typeName, MemberFlags.Public, type)
                                .WithTypeAsRecord()
                                .WithTypeAsStruct()
                                .WithInheritsFrom(CsharpTypeReference.ToRaw($"{backingType}Id"))
                                .WithPrimaryConstructor();

        stronglyTypedId.Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToRaw(typeName), "Empty")
            .Body.Append("return new(Guid.Empty);");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToRaw(typeName), "New")
            .Body.Append("return new(Guid.NewGuid());");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString")
            .Body.Append("return Id.ToString();");
    }

    private static void CreateId(IntermediateNamespaceDefintion ns,
        TypeDefinition def,
        string backingType, 
        string empty,
        string random,
        string toString)
    {
        var typeName = def.GetTypeName();

        var stronglyTypedId = ns
                                .Type(typeName, MemberFlags.Public, CsharpTypeReference.ToRaw(typeName))
                                .WithTypeAsRecord()
                                .WithTypeAsStruct()
                                .WithInheritsFrom(CsharpTypeReference.ToRaw($"{backingType}Id"))
                                .WithPrimaryConstructor();

        stronglyTypedId.Property(MemberFlags.Public, CsharpTypeReference.ToRaw(backingType), "Id");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToRaw(typeName), "Empty")
            .Body.Append($"return {empty};");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToRaw(typeName), "New")
            .Body.Append($"return {random};");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString")
            .Body.Append($"return {toString};");
    }

    private static void CreateStringId(IntermediateNamespaceDefintion ns, string typeName, string backingType, CsharpTypeReference type)
    {
        var stronglyTypedId = ns
                                .Type(typeName, MemberFlags.Public, type)
                                .WithTypeAsRecord()
                                .WithTypeAsStruct()
                                .WithInheritsFrom(CsharpTypeReference.ToRaw($"{backingType}Id"))
                                .WithPrimaryConstructor();

        stronglyTypedId.Property(MemberFlags.Public, CsharpTypeReference.ToType<string>(), "Id");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToType<string>(), "Empty")
            .Body.Append("return string.Empty;");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Static, CsharpTypeReference.ToType<string>(), "New")
            .Body.Append("return Guid.NewGuid().ToString();");

        stronglyTypedId
            .Method(MemberFlags.Public | MemberFlags.Override, CsharpTypeReference.ToType<string>(), "ToString")
            .Body.Append("return Id;");
    }

    private static void CreateStronglyTypedIdInterface(IntermediateNamespaceDefintion ns, StronglyTypedIdAttribute attribute)
    {
        var type = ns
            .Type($"{attribute.Type.Name}Id", MemberFlags.Public, CsharpTypeReference.ToRaw($"{attribute.Type.Name}Id"))
            .WithTypeAsInterface();

        type
            .Property(MemberFlags.Public, CsharpTypeReference.ToType(attribute.Type), "Id")
            .WithGetter();
    }
}

public static class StronglyTypedIdModuleExtensions
{
    public static T WithStronglyTypedIdGenerator<T>(this T project, Action<StronglyTypedIdModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
    {
        var mod = new StronglyTypedIdModule(project);

        if (configure != null)
            configure(mod);

        project.GeneratorModules.Add(mod);

        return project;
    }

    public static T WithJsonConverterForStronglyTypedIdGenerator<T>(this T project, Action<StronglyTypedIdJsonConverterModule>? configure = null)
        where T : IFeatureGroup<CSharpCodeGenerator>
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
        where T : IFeatureGroup<CSharpCodeGenerator>
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