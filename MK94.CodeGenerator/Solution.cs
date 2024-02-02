using MK94.CodeGenerator.Intermediate.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator;

public class Solution
{
    public IReadOnlyList<FileDefinition> AllFiles { init; get; }
    public Extensions.DependencyLookupCache LookupCache { init; get; }

    public string? BasePath { get; set; }

    public static Solution FromAssemblyContaining<Type>()
    {
        var all = new Parser(null).ParseFromAssemblyContainingType<Type>();

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

    public CSharpProject CSharpProject(string? path = null)
    {
        return new CSharpProject
        {
            RelativePath = Path.Combine(BasePath ?? string.Empty, path ?? string.Empty)
        };
    }
}
