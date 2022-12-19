using MK94.CodeGenerator.Generator;
using NUnit.Framework;
using MK94.Assert;
using System.Linq;
using System.Text;

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
            var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<DirectGeneratorTests>();

            new CSharpControllerClientGenerator().Generate(CodeBuilder.FactoryFromMemoryStream(out var files), @"space", controllerFeature);

            CodeBuilder.FlushAll();

            foreach(var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()));
            }
        }
    }
}