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
    public void Test()
    {
        // DiskAssert.EnableWriteMode();

        var c = new TypescriptCodeGenerator();

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
        // DiskAssert.EnableWriteMode();

        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        var csharpCode = new TypescriptCodeGenerator();

        var project = solution
            .TypescriptProject()
            .WhichImplements(controllerFeature)

            .WithPropertiesGenerator()

            .GenerateTo(csharpCode);

        csharpCode.AssertMatches();
    }

    [Test]
    public void DataAndSerializerMixedModuleTest()
    {
        // DiskAssert.EnableWriteMode();

        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        var csharpCode = new TypescriptCodeGenerator();

        var project = solution
            .TypescriptProject()
            .WhichImplements(controllerFeature)
            
            .WithPropertiesGenerator()
            .WithHttpClientModuleGenerator()

            .GenerateTo(csharpCode);

        csharpCode.AssertMatches();
    }
}
