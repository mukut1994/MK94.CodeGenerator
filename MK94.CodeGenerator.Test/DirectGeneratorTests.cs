using MK94.CodeGenerator.Generator;
using NUnit.Framework;
using MK94.Assert;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Test.Controller;

namespace MK94.CodeGenerator.Test
{
    public class DirectGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            // DiskAssert.EnableWriteMode();

            var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<DirectGeneratorTests>();

            new CSharpControllerClientGenerator().Generate(CodeBuilder.FactoryFromMemoryStream(out var files), @"space", controllerFeature);

            CodeBuilder.FlushAll();

            foreach(var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()).Replace("\r\n", "\n"));
            }
        }

        [Test]
        public void Test2()
        {
            // DiskAssert.EnableWriteMode();

            var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<DirectGeneratorTests>();

            var all = new Parser().ParseFromAssemblyContainingType<Page>();
            var cache = all.BuildCache();

            new CSharpDataGenerator(true).Generate(CodeBuilder.FactoryFromMemoryStream(out var files), @"data",
                controllerFeature.GetMethodDependencies(cache).ToFileDef(cache).ToList());

            CodeBuilder.FlushAll(true);

            foreach (var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()).Replace("\r\n", "\n"));
            }
        }
    }
}