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
public class FileAttribute : FeatureAttribute
{
    public string Name { get; set; }

    public FileAttribute(string name)
    {
        Name = name;
    }
}

public static class FileNameFeature
{
    public static T WithFilename<T>(this T group, string name)
        where T : IFeatureGroup
    {
        return group.WithFilename(_ => name);
    }

    /// <summary>
    /// Includes a .g in files before the extension. <br />
    /// e.g. data.cs => data.g.cs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="group"></param>
    /// <returns></returns>
    public static T WithFilenameDotGPostFix<T>(this T group)
        where T : IFeatureGroup
    {
        return group.WithFilename(x => x != null ? $"{x}.g" : null);
    }

    public static T WithFilename<T>(this T group, Func<string?, string?> name)
        where T : IFeatureGroup
    {
        foreach (var file in group.Files)
        {
            var attribute = file.GetFeature<FileAttribute>();
            var update = name(attribute?.Name);

            if (update == null) continue;

            file.FeatureMarks.Set(new FileAttribute(update));
        }

        return group;
    }

    public static string GetFilename(this FileDefinition file)
    {
        return file.GetRequiredFeature<FileAttribute>().Name;
    }
}
