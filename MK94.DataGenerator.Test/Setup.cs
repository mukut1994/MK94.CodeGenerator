using MK94.Assert;
using MK94.Assert.NUnit;
using MK94.Assert.Output;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Test
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SetupDiskAssert.WithRecommendedSettings("MK94.DataGenerator.Test", "../TestData");

            DiskAsserter.Default.PathResolver = PathResolver.Instance;

            // Remove the comment to update/fix tests
            // AssertConfigure.EnableWriteMode();
        }

    }

    [DebuggerStepThrough]
    public class PathResolver : IPathResolver
    {
        public static PathResolver Instance = new PathResolver();

        private static NUnitPathResolver pathResolver = new();

        public static Action? SetContext { get; set; }

        public string GetStepPath()
        {
            if (SetContext != null)
                SetContext();

            return pathResolver.GetStepPath();
        }
    }
}
