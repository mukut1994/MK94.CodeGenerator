using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.Typescript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MK94.CodeGenerator;

public class Solution : IFeatureGroup
{
    // TODO Hacky fix to pass through typescript type lookups
    public Dictionary<Type, string> TypescriptTypeLookups = new();

    Solution IFeatureGroup.Solution => this;

    public record ProjectIdentifier(string? Path, Type type);

    public IReadOnlyList<FileDefinition> AllFiles { init; get; }

    public Extensions.DependencyLookupCache LookupCache { init; get; }

    public Dictionary<ProjectIdentifier, Project> Projects { get; set; } = new();

    public string? BasePath { get; set; }

    // TODO just rename the property and maybe the interface?
    List<FileDefinition> IFeatureGroup.Files => AllFiles.ToList();

    public static Solution From(List<FileDefinition> files)
    {
        return new Solution
        {
            AllFiles = files,
            LookupCache = files.BuildCache()
        };
    }

    public static Solution FromAssemblyContaining<Type>()
    {
        var all = new Parser().ParseFromAssemblyContainingType<Type>();

        return new Solution
        {
            AllFiles = all,
            LookupCache = all.BuildCache()
        };
    }

    public Solution WithBasePath(string basePath)
    {
        BasePath = basePath;
        return this;
    }

    public ICSharpProject CSharpProject(string? path = null)
    {
        return Project(path, () => new CSharpProject(this, path ?? string.Empty));
    }

    public ITypescriptProject TypescriptProject(string? path = null)
    {
        return Project(path, () => new TypescriptProject(this, path ?? string.Empty));
    }

    public T Project<T>(string? path, Func<T> project)
        where T : Project
    {
        return (T) Projects.GetOrAdd(ToIdentifier<CSharpProject>(path), project);
    }

    private ProjectIdentifier ToIdentifier<T>(string? path)
    {
        return new ProjectIdentifier(path, typeof(T));
    }

    public void GenerateTo(Func<string, CodeBuilder> output)
    {
        foreach (var project in Projects)
            project.Value.Generate(output);
    }

    public Dictionary<string, MemoryStream> GenerateToMemory()
    {
        var output = CodeBuilder.FactoryFromMemoryStream(out var files);

        GenerateTo(output);

        return files;
    }

    public void GenerateToDisk()
    {
        var output = CodeBuilder.FactoryFromBasePath(BasePath ?? string.Empty);

        GenerateTo(output);
    }
}
