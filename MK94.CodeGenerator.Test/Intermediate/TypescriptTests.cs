using MK94.Assert;
using MK94.CodeGenerator.Generator;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;
using MK94.CodeGenerator.Intermediate.Typescript;
using MK94.CodeGenerator.Intermediate.Typescript.Modules;
using MK94.CodeGenerator.Test.Controller;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test.Intermediate;

public class TypescriptTests
{
    [Test]
    public void TestTs()
    {
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

    [Test]
    public void Test()
    {
        var c = new TypescriptCodeGenerator(new(new()));

        var t = c
            .File("file.ts")
            .Type("TypeA", MemberFlags.Public);

        t.Property(MemberFlags.Public, TsTypeReference.ToType<int>(), "PropA");
        t.Method(MemberFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
            .WithArgument(TsTypeReference.ToType<int>(), "a")
            .WithArgument(TsTypeReference.ToType<int>(), "b")
            .Body
            .Append("return a + b;");


        var t2 = c
            .File("file.ts")
            .Type("TypeB", MemberFlags.Public);

        t2.Property(MemberFlags.Public, TsTypeReference.ToType<int>(), "PropA");
        t2.Method(MemberFlags.Public, TsTypeReference.ToType<int>(), "MethodA")
            .WithArgument(TsTypeReference.ToType<int>(), "c")
            .WithArgument(TsTypeReference.ToType<int>(), "d")
            .Body
            .Append("return c + d;");


        c.AssertMatches();
    }


    [Test]
    public void DataModuleTest()
    {
        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        var typescriptCode = new TypescriptCodeGenerator(new(new()));

        var project = solution
            .TypescriptProject()
            .WhichImplements(controllerFeature)

            .WithPropertiesGenerator()

            .GenerateTo(typescriptCode);

        typescriptCode.AssertMatches();
    }

    [Test]
    public void DataAndSerializerMixedModuleTest()
    {
        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        var typescriptCode = new TypescriptCodeGenerator(new(solution.AllFiles.ToList()));

        var project = solution
            .TypescriptProject()
            .WhichImplements(controllerFeature)
            
            .WithPropertiesGenerator()
            .WithEnumsGenerator()
            .WithFetchClientModuleGenerator()

            .GenerateTo(typescriptCode);

        typescriptCode.AssertMatches(IndentStyle.SameLine);
    }
}
