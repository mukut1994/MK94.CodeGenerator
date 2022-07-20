using MK94.DataGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Test
{
    public class ControllerFeature : ProjectAttribute
    {
        private const string Name = nameof(ControllerFeature);

        public static Parser Parser = new Parser(Name);

        public ControllerFeature() : base(Name) { }
    }
}
