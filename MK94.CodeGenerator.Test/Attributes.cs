using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test
{
    public class ControllersProject : ProjectAttribute
    {
        private const string Name = nameof(ControllersProject);

        public static Parser Parser = new Parser(new ParserConfig() { Project = Name, MandatoryFileAttribute = true });

        public ControllersProject() : base(Name) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
    public class ExampleAttribute : Attribute
    {
    }
}
