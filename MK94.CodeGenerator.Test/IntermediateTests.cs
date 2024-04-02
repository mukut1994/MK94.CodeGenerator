using MK94.Assert;
using MK94.CodeGenerator.Generator;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.Typescript;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test
{
    public class IntermediateTests
    {
        [Test]
        public void TestTs()
        {
            // DiskAssert.EnableWriteMode();

            var c = new TypescriptCodeGenerator(new(new()));

            var t = c
                .File("filea.ts")
                .Type("TypeA", MemberFlags.Public);

            t.Property(MemberFlags.Public, TsTypeReference.ToType<int>(), "PropA");
            t.Method(MemberFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(TsTypeReference.ToType<int>(), "a")
                .WithArgument(TsTypeReference.ToType<int>(), "b")
                .Body
                .Append("return a + b;");


            var t2 = c
                .File("fileb.ts")
                .Type("TypeB", MemberFlags.Public);

            t2.Property(MemberFlags.Public, TsTypeReference.ToType<int>(), "PropA");
            t2.Method(MemberFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(TsTypeReference.ToRaw("TypeA"), "c")
                .WithArgument(TsTypeReference.ToRaw("TypeA"), "d")
                .Body
                .Append("return c + d;");

            c.Generate(CodeBuilder.FactoryFromMemoryStream(out var files, IndentStyle.SameLine));

            CodeBuilder.FlushAll();

            foreach (var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()).Replace("\r\n", "\n"));
            }
        }
    }
}
