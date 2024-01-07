using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace data
{
    public class Page
    {
        [FromQuery]
        public required Int32 Size { get; set; }
        [FromQuery]
        public required Int32 Index { get; set; }
    }
}
