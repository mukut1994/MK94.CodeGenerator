using MK94.Assert;
using MK94.CodeGenerator.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test;

public static class Extensions
{
    public static void AssertMatches(this IFileGenerator generator)
    {
        generator.Generate(CodeBuilder.FactoryFromMemoryStream(out var files));

        CodeBuilder.FlushAll();

        foreach (var file in files)
        {
            DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()).Replace("\r\n", "\n"));
        }
    }
}
