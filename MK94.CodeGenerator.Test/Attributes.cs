using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test
{
    public class ControllerFeature : ProjectAttribute
    {
        private const string Name = nameof(ControllerFeature);

        public static Parser Parser = new Parser(new ParserConfig() { Project = Name, MandatoryFileAttribute = true });

        public ControllerFeature() : base(Name) { }
    }
}
