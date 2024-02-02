using MK94.Assert;
using MK94.CodeGenerator.Generator;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;
using MK94.CodeGenerator.Test.Controller;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test.Intermediate;

public class CSharpTests
{
    [Test]
    public void Test()
    {
        var c = new CSharpCodeGenerator();

        var t = c
            .File("file.cs")
            .Namespace("Namespace.A")
            .Type("TypeA", MemberFlags.Public);

        t.Property(MemberFlags.Public, CsTypeReference.ToType<int>(), "PropA");
        t.Method(MemberFlags.Public, CsTypeReference.ToType<int>(), "MethodA")
            .WithArgument(CsTypeReference.ToType<int>(), "a")
            .WithArgument(CsTypeReference.ToType<int>(), "b")
            .Body
            .Append("return a + b;");


        var t2 = c
            .File("file.cs")
            .Namespace("Namespace.B")
            .Type("TypeB", MemberFlags.Public);

        t2.Property(MemberFlags.Public, CsTypeReference.ToType<int>(), "PropA");
        t2.Method(MemberFlags.Public, CsTypeReference.ToType<int>(), "MethodA")
            .WithArgument(CsTypeReference.ToType<int>(), "c")
            .WithArgument(CsTypeReference.ToType<int>(), "d")
            .Body
            .Append("return c + d;");


        c.AssertMatches();
    }


    [Test]
    public void DataModuletest()
    {
        DiskAssert.EnableWriteMode();

        var all = new Parser(null).ParseFromAssemblyContainingType<Page>();
        var cache = all.BuildCache();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<DirectGeneratorTests>();

        var project = new Project()
        {
            Files = controllerFeature.GetMethodDependencies(cache).ToFileDef(cache).ToList()
        };

        var csharpCode = new CSharpCodeGenerator();

        DataModule.Using(project).AddTo(csharpCode);

        csharpCode.AssertMatches();
    }

    [Test]
    public void DataAndSerializerMixedModuletest()
    {
        DiskAssert.EnableWriteMode();

        var all = new Parser(null).ParseFromAssemblyContainingType<Page>();
        var cache = all.BuildCache();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<DirectGeneratorTests>();

        var project = new Project()
        {
            Files = controllerFeature.GetMethodDependencies(cache).ToFileDef(cache).ToList()
        };

        var csharpCode = new CSharpCodeGenerator();

        DataModule.Using(project).AddTo(csharpCode);
        JsonToStringModule.Using(project).AddTo(csharpCode);

        csharpCode.AssertMatches();
    }
}
