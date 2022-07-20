using MK94.DataGenerator.Generator;
using NUnit.Framework;
using MK94.Assert;
using System.Linq;
using System.Text;

namespace MK94.DataGenerator.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            DiskAssert.EnableWriteMode();

            var controllerFeature = ControllerFeature.Parser.ParseFromAssemblyContainingType<Tests>();

            new CSharpControllerClientGenerator().Generate(CodeBuilder.FactoryFromMemoryStream(out var files), @"space", controllerFeature);

            CodeBuilder.FlushAll();

            foreach(var file in files)
            {
                DiskAssert.MatchesRaw(file.Key, Encoding.UTF8.GetString(file.Value.ToArray()));
            }
        }
    }
}