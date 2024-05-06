using MK94.CodeGenerator.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Features;

/// <summary>
/// The file name where this class is located.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public class TypeNameAttribute : FeatureAttribute
{
    public string Name { get; set; }

    public TypeNameAttribute(string name)
    {
        Name = name;
    }
}

public static class TypeNameFeature
{
    public static T WithTypeName<T>(this T group, string name)
        where T : IFeatureGroup
    {
        return group.WithTypeName(_ => name);
    }

    public static T WithTypeName<T>(this T group, Func<string?, string?> name)
        where T : IFeatureGroup
    {
        foreach (var file in group.Files)
        {
            foreach (var type in file.Types.Cast<IFeatureMarked>().Concat(file.EnumTypes))
            {
                var attribute = type.GetFeature<TypeNameAttribute>();
                var update = name(attribute?.Name);

                if (update == null) continue;

                type.FeatureMarks.Set(new TypeNameAttribute(update));
            }
        }

        return group;
    }

    public static string GetTypeName(this TypeDefinition type)
    {
        return type.GetRequiredFeature<TypeNameAttribute>().Name;
    }
}
