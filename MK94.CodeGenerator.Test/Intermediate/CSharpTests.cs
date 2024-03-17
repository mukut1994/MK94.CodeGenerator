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
        // DiskAssert.EnableWriteMode();

        var c = new CSharpCodeGenerator();

        var namespaceA = c
            .File("file.cs")
            .WithUsing("System")
            .Namespace("Namespace.A");

        namespaceA
            .Type("IId", MemberFlags.Public)
            .WithTypeAsInterface()
            .Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id")
            .WithGetter();

        namespaceA
            .Type("StructA", MemberFlags.Public)
            .WithTypeAsRecord()
            .WithTypeAsStruct()
            .WithInheritsFrom(CsharpTypeReference.ToRaw("IId"));

        var typeA = namespaceA.Type("TypeA", MemberFlags.Public);

        typeA.Attribute(CsharpTypeReference.ToType<ExampleAttribute>());

        var propA = typeA.Property(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "PropA");
        propA.WithDefaultExpression(" = 0;");
        propA.Attribute(CsharpTypeReference.ToType<ExampleAttribute>());

        var method = typeA.Method(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "MethodA")
            .WithArgument(CsharpTypeReference.ToType<int>(), "a")
            .WithArgument(CsharpTypeReference.ToType<int>(), "b");

        method.Attribute(CsharpTypeReference.ToType<ExampleAttribute>());

        method.Body
            .Append("return a + b;");

        typeA.Type("TypeASubType", MemberFlags.Public);

        var t2 = c
            .File("file.cs")
            .Namespace("Namespace.B")
            .Type("TypeB", MemberFlags.Public);

        t2.Property(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "PropA");
        t2.Method(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "MethodA")
            .WithArgument(CsharpTypeReference.ToType<int>(), "c")
            .WithArgument(CsharpTypeReference.ToType<int>(), "d")
            .Body
            .Append("return c + d;");


        c.AssertMatches();
    }


    [Test]
    public void DataModuletest()
    {
        // DiskAssert.EnableWriteMode();

        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<Page>();

        var csharpCode = new CSharpCodeGenerator();

        var project = solution
            .CSharpProject()
            .WhichImplements(controllerFeature)
            .WithinNamespace("TestNameSpace")

            .WithPropertiesGenerator()

            .GenerateTo(csharpCode);

        csharpCode.AssertMatches();
    }

    [Test]
    public void DataModule_StronglyTypedId()
    {
        DiskAssert.EnableWriteMode();

        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<Page>();

        var csharpCode = new CSharpCodeGenerator();

        var project = solution
            .CSharpProject()
            .WhichImplements(controllerFeature)
            .WithinNamespace("TestNameSpace")

            .WithPropertiesGenerator()
            .WithStronglyTypedIdGenerator()

            .GenerateTo(csharpCode);

        csharpCode.AssertMatches();
    }

    [Test]
    public void DataAndSerializerMixedModuleTest()
    {
        // DiskAssert.EnableWriteMode();

        var solution = Solution.FromAssemblyContaining<Page>();

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<Page>();

        var csharpCode = new CSharpCodeGenerator();

        var project = solution
            .CSharpProject()
            .WhichImplements(controllerFeature)
            .WithinNamespace("TestNameSpace")
            
            .WithPropertiesGenerator()
            .WithJsonToStringGenerator()
            .WithFlurlClientGenerator()
            
            .GenerateTo(csharpCode);

        csharpCode.AssertMatches();
    }
}
