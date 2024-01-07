using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test.Controller
{
    [File("Data")]
    public class Page
    {
        [Query]
        public int Size { get; set; }

        [Query]
        public int Index { get; set; }
    }

    [ControllerFeature]
    [File("Controller")]
    public interface IPizzaController
    {
        [Get]
        Task PizzaList([Query] Page page);
    }
}
