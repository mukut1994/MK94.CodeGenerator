using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Features;

public class NamespaceFeature : FeatureAttribute
{
    public string Name { get; }

    public NamespaceFeature(string name)
    {
        Name = name;
    }
}

public static class NamespaceFeatureExtensions
{
    /// <summary>
    /// Set namespaces to match the exact same namespaces as where the types are defined.
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static Solution WithGeneratorNamespaces(this Solution solution,
        Predicate<IFeatureMarked>? filter = null)
    {
        filter ??= _ => true;

        foreach (var type in solution.AllFiles.SelectMany(x => x.Types))
        {
            if (!filter(type))
                continue;

            type.FeatureMarks.Set(new NamespaceFeature(type.Type.Namespace!));
        }

        foreach (var type in solution.AllFiles.SelectMany(x => x.EnumTypes))
        {
            type.FeatureMarks.Set(new NamespaceFeature(type.Type.Namespace!));
        }

        return solution;
    }

    public static T WithinNamespace<T>(this T group, string space)
        where T : IFeatureGroup
    {
        foreach (var file in group.Files)
        {
            file.ForeachFeatureMarked(x => x.FeatureMarks.Set(new NamespaceFeature(space)), x => x is FileDefinition);
        }

        return group;
    }

    public static string GetNamespace(this IFeatureMarked type)
    {
        return type.GetRequiredFeature<NamespaceFeature>().Name;
    }
}
