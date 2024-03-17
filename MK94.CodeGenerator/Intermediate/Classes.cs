using MK94.CodeGenerator.Intermediate.CSharp;
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

public interface ICodeGenerator<T, TRet>
    where T : IFileGenerator
{
    TRet GenerateTo(T target);
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

public interface INamespaceResolver<T>
{
    Func<TypeDefinition, string> NamespaceResolver { get; set; }
}

public static class NamespaceResolverExtensions
{
    public static ICSharpProject WithinNamespace(this ICSharpProject type, string @namespace)
    {
        type.NamespaceResolver = _ => @namespace;

        return type;
    }
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
    Getter = 1,
    Setter = 2,
    Init = 4,
}

public interface IProject
{
    Solution Solution { get; init; }
   
    List<FileDefinition> Files { get; set; }
    
    List<Project> References { get; set; }

}

public class Project : IProject
{
    public Solution Solution { get; init; }

    public List<FileDefinition> Files { get; set; } = new();

    public List<Project> References { get; set;} = new();

    public Project(Solution solution)
    {
        Solution = solution;
    }
}

public static class ProjectExtensions
{
    public static T WhichImplements<T>(this T project, List<FileDefinition> files)
        where T : IProject
    {
        var deps = files.GetMethodDependencies(project.Solution.LookupCache).ToFileDef(project.Solution.LookupCache).ToList();

        project.Files.AddRange(deps);
        project.Files.AddRange(files);

        return project;
    }
}