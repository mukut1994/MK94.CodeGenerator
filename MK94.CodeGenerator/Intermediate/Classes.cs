using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate;

public interface IFileGenerator
{
    void Generate(Func<string, CodeBuilder> factory);
}

public interface IGeneratorModule<T>
    where T : IFileGenerator
{
    void AddTo(T codeGenerator);
}

public interface IGeneratorModuleUser<T>
    where T : IFileGenerator
{
    List<IGeneratorModule<T>> GeneratorModules { get; }

}

[Flags]
public enum MemberFlags
{
    Public = 1,
    Static = 2,
    Override = 4,

    Type = 8,
    Interface = 16,
    Async = 32,
    Method = 64,

    Partial = 128,
    Const = 256,
}

[Flags]
public enum DefinitionType
{
    Default = 0,
    Class = 1,
    Record = 2,
    Struct = 4,
    Interface = 8,
}


[Flags]
public enum PropertyType
{
    Default = 0,
    Getter = 1,
    Setter = 2,
    Init = 4,
}

public interface IProject
{
    Solution Solution { get; init; }

    [Obsolete]
    List<FileDefinition> Files { get; set; }

}

public interface IFeatureGroup
{
    Solution Solution { get; }

    List<FileDefinition> Files { get; }
}

public interface IFeatureGroup<TGenerator> : IFeatureGroup
    where TGenerator : IFileGenerator
{
    List<IGeneratorModule<TGenerator>> GeneratorModules { get; }

    new List<FileDefinition> Files { get; set; }
}

public class FeatureGroup<TGenerator> : IFeatureGroup<TGenerator> 
    where TGenerator : IFileGenerator
{
    public required Solution Solution { get; init; }
 
    public List<FileDefinition> Files { get; set; } = new();

    public List<IGeneratorModule<TGenerator>> GeneratorModules { get; } = new();
}

public abstract class Project : IProject
{
    public Solution Solution { get; init; }

    [Obsolete]
    public List<FileDefinition> Files { get; set; } = new();

    public Project(Solution solution)
    {
        Solution = solution;
    }

    public abstract void Generate(Func<string, CodeBuilder> outputFactory);
}

public abstract class Project<TGenerator> : Project
    where TGenerator : IFileGenerator
{
    public List<FeatureGroup<TGenerator>> FeatureGroups { get; set; } = new();

    public Project(Solution solution) : base(solution)
    {
    }
}

public static class ProjectExtensions
{
    public static IFeatureGroup<CSharpCodeGenerator> UsesAllSolutionFeatures(this ICSharpProject project)
    {
        var ret = new FeatureGroup<CSharpCodeGenerator>()
        {
            Solution = project.Solution,
            Files = project.Solution.AllFiles.ToList(),
        };

        project.FeatureGroups.Add(ret);

        return ret;
    }

    public static IFeatureGroup<CSharpCodeGenerator> Uses<T>(this ICSharpProject project)
        where T : FeatureAttribute
    {
        var ret = new FeatureGroup<CSharpCodeGenerator>()
        {
            Solution = project.Solution,
            Files = project.FindWithFeatures<T>(),
        };

        project.FeatureGroups.Add(ret);

        return ret;
    }

    public static IFeatureGroup<CSharpCodeGenerator> Excluding<T>(this IFeatureGroup<CSharpCodeGenerator> group)
        where T : FeatureAttribute
    {
        var featureFiles = group.Solution.AllFiles.FindWithFeatures<T>(group.Solution.LookupCache);

        var deps = group.Files.ExcludeAndInheritFrom(featureFiles).ToList();

        group.Files = deps;

        return group;
    }

    public static IFeatureGroup<CSharpCodeGenerator> UsesDependenciesOf<T>(this ICSharpProject project)
        where T : FeatureAttribute
    {
        var featureFiles = project.Solution.AllFiles.FindWithFeatures<T>(project.Solution.LookupCache);

        var deps = featureFiles.GetMethodDependencies(project.Solution.LookupCache).ToFileDef(project.Solution.LookupCache).ToList();

        var ret = new FeatureGroup<CSharpCodeGenerator>()
        {
            Solution = project.Solution,
            Files = deps,
        };

        project.FeatureGroups.Add(ret);

        return ret;
    }

    [Obsolete]
    public static T WhichUses<T>(this T project, List<FileDefinition> files)
        where T : IProject
    {
        project.Files.AddRange(files);

        return project;
    }

    [Obsolete]
    public static T WhichImplements<T>(this T project, List<FileDefinition> files)
        where T : IProject
    {
        var deps = files.GetMethodDependencies(project.Solution.LookupCache).ToFileDef(project.Solution.LookupCache).ToList();

        project.Files.AddRange(deps);
        project.Files.AddRange(files);

        return project;
    }

    [Obsolete]
    public static T WithDependenciesFor<T>(this T project, List<FileDefinition> files)
        where T : IProject
    {
        var deps = files.GetMethodDependencies(project.Solution.LookupCache).ToFileDef(project.Solution.LookupCache).ToList();

        project.Files.AddRange(deps);

        return project;
    }

    [Obsolete]
    public static T Without<T>(this T project, List<FileDefinition> files)
        where T : IProject
    {
        project.Files = project.Files.ExcludeAndInheritFrom(files).ToList();
        
        return project;
    }
}