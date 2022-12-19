using MK94.Assert;
using MK94.DataGenerator.Generator;
using MK94.DataGenerator.Intermediate.CSharp;
using MK94.DataGenerator.Intermediate.Typescript;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Test
{
    public class IntermediateTests
    {
        [Test]
        public void Test()
        {
            var c = new CSharpCodeGenerator();

            var t = c
                .File("file.cs")
                .Namespace("Namespace.A")
                .Type("TypeA", System.Reflection.BindingFlags.Public);

            t.Property(System.Reflection.BindingFlags.Public, CsTypeReference.ToType<int>(), "PropA");
            t.Method(System.Reflection.BindingFlags.Public, CsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(CsTypeReference.ToType<int>(), "a")
                .WithArgument(CsTypeReference.ToType<int>(), "b")
                .Body
                .Append("return a + b;");


            var t2 = c
                .File("file.cs")
                .Namespace("Namespace.B")
                .Type("TypeB", System.Reflection.BindingFlags.Public);

            t2.Property(System.Reflection.BindingFlags.Public, CsTypeReference.ToType<int>(), "PropA");
            t2.Method(System.Reflection.BindingFlags.Public, CsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(CsTypeReference.ToType<int>(), "c")
                .WithArgument(CsTypeReference.ToType<int>(), "d")
                .Body
                .Append("return c + d;");

            c.Generate(CodeBuilder.FactoryFromMemoryStream(out var files));

            CodeBuilder.FlushAll();

            foreach (var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()));
            }
        }


        [Test]
        public void TestTs()
        {
            var c = new TypescriptCodeGenerator();

            var t = c
                .File("filea.ts")
                .Type("TypeA", System.Reflection.BindingFlags.Public);

            t.Property(System.Reflection.BindingFlags.Public, TsTypeReference.ToType<int>(), "PropA");
            t.Method(System.Reflection.BindingFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(TsTypeReference.ToType<int>(), "a")
                .WithArgument(TsTypeReference.ToType<int>(), "b")
                .Body
                .Append("return a + b;");


            var t2 = c
                .File("fileb.ts")
                .Type("TypeB", System.Reflection.BindingFlags.Public);

            t2.Property(System.Reflection.BindingFlags.Public, TsTypeReference.ToType<int>(), "PropA");
            t2.Method(System.Reflection.BindingFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
                .WithArgument(TsTypeReference.ToRaw("TypeA"), "c")
                .WithArgument(TsTypeReference.ToRaw("TypeA"), "d")
                .Body
                .Append("return c + d;");

            c.Generate(CodeBuilder.FactoryFromMemoryStream(out var files, IndentStyle.SameLine));

            CodeBuilder.FlushAll();

            foreach (var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()));
            }
        }
    }
}
