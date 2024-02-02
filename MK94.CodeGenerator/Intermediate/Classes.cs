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

[Flags]
public enum MemberFlags
{
    Public = 1,
    Static = 2,
    Override = 4
}

public class Project
{
    public List<FileDefinition> Files { get; set; } = new();
}


public static class ProjectExtensions
{
    public static T WithData<T>(this T project, List<FileDefinition> files)
        where T : Project
    {
        project.Files.AddRange(files);

        return project;
    }
}