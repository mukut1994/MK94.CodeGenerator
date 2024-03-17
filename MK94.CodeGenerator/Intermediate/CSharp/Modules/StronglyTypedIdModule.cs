using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Modules;

public class StronglyTypedIdModule : IGeneratorModule<CSharpCodeGenerator>
{
    private readonly ICSharpProject project;

    public StronglyTypedIdModule(ICSharpProject project)
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

                var propertiesWithStronglyTypedAttribute = typeDef.Properties.Where(x => x.Info.GetCustomAttributes<StronglyTypedIdAttribute>().Any()).ToList();

                if (propertiesWithStronglyTypedAttribute.Count == 0)
                    continue;

                var ns = file.Namespace(project.NamespaceResolver(typeDef));
                var type = CreateStronglyTypedIdInterface(ns);

                foreach (var property in propertiesWithStronglyTypedAttribute)
                {
                    var stronglyTypedId = ns
                        .Type(property.Name, MemberFlags.Public)
                        .WithTypeAsRecord()
                        .WithTypeAsStruct()
                        .WithInheritsFrom(CsharpTypeReference.ToRaw("IId"))
                        .WithPrimaryConstructor();

                    stronglyTypedId.Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id");
                }
            }
        }
    }

    private static IntermediateFileDefinition.IntermediateTypeDefinition CreateStronglyTypedIdInterface(IntermediateFileDefinition.IntermediateNamespaceDefintion ns)
    {
        var type = ns.Type("IId", MemberFlags.Public).WithTypeAsInterface();

        type.Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id").WithGetter();

        return type;
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
}

[AttributeUsage(AttributeTargets.Property)]
public class StronglyTypedIdAttribute : Attribute
{
}