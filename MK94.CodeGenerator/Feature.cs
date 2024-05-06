using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator;

public static class FeatureExtensions
{
    public static IReadOnlyCollection<IFeatureMarked> ForeachFeatureMarked(
        this IReadOnlyCollection<IFeatureMarked> marked,
        Action<IFeatureMarked> action,
        Predicate<IFeatureMarked>? filter = null)
    {
        foreach (var file in marked)
        {
            file.ForeachFeatureMarked(action, filter);
        }

        return marked;
    }

    public static Solution ForeachFeatureMarked(this Solution solution,
        Action<IFeatureMarked> action,
        Predicate<IFeatureMarked>? filter = null)
    {
        solution.AllFiles.ForeachFeatureMarked(action, filter);

        return solution;
    }

    public static void ForeachFeatureMarked(this IFeatureMarked marked,
        Action<IFeatureMarked> action,
        Predicate<IFeatureMarked>? filter = null)
    {
        filter ??= (_) => true;

        marked.InternalForeachFeatureMarked(action, filter);
    }

    private static void InternalForeachFeatureMarked(this IFeatureMarked marked,
        Action<IFeatureMarked> action,
        Predicate<IFeatureMarked> filter)
    {
        if (filter(marked))
            action(marked);

        foreach (var child in marked.FeatureMarkedChildren)
            child.ForeachFeatureMarked(action);
    }

    /// <summary>
    /// Load features by reading the attributes on each member.
    /// </summary>
    public static Solution WithFeaturesFromAttributes(this Solution solution)
    {
        foreach (var file in solution.AllFiles)
        {
            file.ForeachFeatureMarked(x => x.FeatureMarks = x.ReadFeatures().ToDictionary(x => x.GetType(), x => x));
        }

        return solution;
    }

    /// <summary>
    /// Set a feature on the collection. If an identical feature exists, it will be overwritten.
    /// </summary>
    public static void Set(this Dictionary<Type, FeatureAttribute> features, FeatureAttribute feature)
    {
        features[feature.GetType()] = feature;
    }

    /// <summary>
    /// Helper method to get a specific feature.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InvalidOperationException"></exception>
    public static T GetRequiredFeature<T>(this IFeatureMarked marked)
        where T : FeatureAttribute
    {
        if (!marked.FeatureMarks.TryGetValue(typeof(T), out var ret))
            throw new InvalidOperationException($"Feature not found {typeof(T).FullName}");

        return (T) ret;
    }

    /// <summary>
    /// Helper method to get a specific feature.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static T? GetFeature<T>(this IFeatureMarked marked)
        where T : FeatureAttribute
    {
        marked.FeatureMarks.TryGetValue(typeof(T), out var ret);

        return (T?)ret;
    }

    public static List<FileDefinition> FindWithFeatures<T>(this IProject project)
        where T : FeatureAttribute
    {
        return project.Solution.AllFiles.FindWithFeatures<T>(project.Solution.LookupCache);
    }

    internal static List<FileDefinition> FindWithFeatures<T>(
        this IReadOnlyList<IFeatureMarked> marked,
        Extensions.DependencyLookupCache cache)
        where T : FeatureAttribute
    {
        var tMarked = new List<IFeatureMarked>();

        marked.ForeachFeatureMarked(x => tMarked.Add(x), x => x.GetFeature<T>() != null);

        var types = new HashSet<Type>();

        foreach(var def in tMarked)
        {
            if (def is TypeDefinition typeDef)
                types.Add(typeDef.Type);
            else if (def is EnumDefintion enumDef)
                types.Add(enumDef.Type);
            else if (def is FileDefinition fileDef)
            {
                foreach (var fTypeDef in fileDef.Types)
                    types.Add(fTypeDef.Type);
                foreach (var eTypeDef in fileDef.EnumTypes)
                    types.Add(eTypeDef.Type);
            }
        }

        return types.ToFileDef(cache);
    }

    public static T WithTransitiveDependencies<T>(T group)
        where T : IFeatureGroup
    {
        var deps = group.Files.GetMethodDependencies(group.Solution.LookupCache).ToFileDef(group.Solution.LookupCache).ToList();

        group.Files.AddRange(deps);

        return group;
    }
}
