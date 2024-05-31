using MK94.Assert;
using MK94.CodeGenerator.Features;
using MK94.CodeGenerator.Generator;
using MK94.CodeGenerator.Intermediate;
using MK94.CodeGenerator.Intermediate.CSharp.Generator;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;
using MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;
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

        var namespaceA = c
            .File("file.cs")
            .WithUsing("System")
            .Namespace("Namespace.A");

        namespaceA
            .Type("IId", MemberFlags.Public, CsharpTypeReference.ToRaw("IId"))
            .WithTypeAsInterface()
            .Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id")
            .WithGetter();

        var recordStruct = namespaceA
            .Type("RecordStructA", MemberFlags.Public, CsharpTypeReference.ToRaw("RecordStructA"))
            .WithTypeAsRecord()
            .WithTypeAsStruct()
            .WithInheritsFrom(CsharpTypeReference.ToRaw("IId"))
            .WithPrimaryConstructor();

        recordStruct.Property(MemberFlags.Public, CsharpTypeReference.ToType<Guid>(), "Id");

        var typeA = namespaceA.Type("TypeA", MemberFlags.Public, CsharpTypeReference.ToRaw("TypeA"));

        var constructorA = typeA.Constructor(MemberFlags.Public);

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

        typeA.Type("TypeASubType", MemberFlags.Public, CsharpTypeReference.ToRaw("TypeASubType"));

        var t2 = c
            .File("file.cs")
            .Namespace("Namespace.B")
            .Type("TypeB", MemberFlags.Public, CsharpTypeReference.ToRaw("TypeB"));

        t2.Property(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "PropA");
        t2
            .Method(MemberFlags.Public, CsharpTypeReference.ToType<int>(), "MethodA")
            .WithArgument(CsharpTypeReference.ToType<int>(), "c")
            .WithArgument(CsharpTypeReference.ToType<int>(), "d")
            .Body.Append("return c + d;");

        c.AssertMatches();
    }


    [Test]
    public void DataModuleTest()
    {
        var solution = Solution.FromAssemblyContaining<Page>()
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix(); ;

        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        solution
            .CSharpProject()
            .Uses<ControllerFeatureAttribute>()
            .WithNamespace("TestNameSpace")
            .WithPropertiesGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }

    [Test]
    public void DataModuleTest_Controller()
    {
        var solution = Solution.FromAssemblyContaining<Page>()
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix(); ;

        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        solution
            .CSharpProject()
            .UsesDependenciesOf<ControllerFeatureAttribute>()
            .WithNamespace("TestNameSpace")
            .WithPropertiesGenerator()
            .WithControllerModuleGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }

    [Test]
    public void DataModuleTest_InterfaceMethodModule()
    {
        var solution = Solution.FromAssemblyContaining<Page>()
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix(); ;

        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        solution
            .CSharpProject()
            .UsesAllSolutionFeatures()
            .Excluding<ControllerFeatureAttribute>()
            .WithNamespace("TestNameSpace")
            .WithInterfaceMethodGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }

    [Test]
    public void DataModule_StronglyTypedId()
    {
        var solution = Solution.FromAssemblyContaining<Page>()
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix(); ;

        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        solution
            .CSharpProject()
            .Uses<ControllerFeatureAttribute>()
            .WithNamespace("TestNameSpace")
            .WithPropertiesGenerator()
            .WithStronglyTypedIdGenerator()
            .WithJsonConverterForStronglyTypedIdGenerator()
            .WithEfCoreValueConverterForStronglyTypedIdGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }

    [Test]
    public void DataAndSerializerMixedModuleTest()
    {
        var solution = Solution.FromAssemblyContaining<Page>()
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix();

        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        solution
            .CSharpProject()
            .Uses<ControllerFeatureAttribute>()
            .WithNamespace("TestNameSpace")
            .WithPropertiesGenerator()
            .WithJsonToStringGenerator()
            .WithFlurlClientGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }

    [Test]
    public void SpecificTypesOnlyTest()
    {
        // TODO parser should be created from solution
        // This requires all files multiple times otherwise
        // The solution should be defining all of them instead
        var allfiles = new Parser().ParseFromTypes(t => "generated", typeof(Page), typeof(PageId));
        var solution = Solution.From(allfiles)
            .WithFeaturesFromAttributes()
            .WithFilenameDotGPostFix(); ;

        // TODO cleaner parser syntax
        var controllerFeature = ControllerFeatureAttribute.Parser.ParseFromAssemblyContainingType<Page>();

        var project = solution
            .CSharpProject()
            .UsesAllSolutionFeatures()
            .WithNamespace("TestNameSpace")
            .WithPropertiesGenerator();

        solution.GenerateToMemory()
            .DecodeUTF8()
            .AssertMatches();
    }
}
