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

public class Solution
{
    public IReadOnlyList<FileDefinition> AllFiles { init; get; }
    public Extensions.DependencyLookupCache LookupCache { init; get; }

    public string? BasePath { get; set; }

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
        return new CSharpProject(this, Path.Combine(BasePath ?? string.Empty, path ?? string.Empty));
    }

    public ITypescriptProject TypescriptProject(string? path = null)
    {
        return new TypescriptProject(this, Path.Combine(BasePath ?? string.Empty, path ?? string.Empty));
    }
}
