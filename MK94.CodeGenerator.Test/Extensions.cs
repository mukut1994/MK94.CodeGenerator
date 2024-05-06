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
    public static void AssertMatches(this IFileGenerator generator, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        generator.Generate(CodeBuilder.FactoryFromMemoryStream(out var files, indentStyle));

        CodeBuilder.FlushAll();

        foreach (var file in files)
        {
            DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()).Replace("\r\n", "\n"));
        }
    }

    public static void AssertMatches(this Dictionary<string, string> files, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        CodeBuilder.FlushAll();

        foreach (var file in files)
        {
            DiskAssert.MatchesRaw(file.Key, file.Value.Replace("\r\n", "\n"));
        }
    }
}
