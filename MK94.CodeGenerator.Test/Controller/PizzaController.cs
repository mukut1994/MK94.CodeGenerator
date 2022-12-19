using MK94.CodeGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Test.Controller
{
    [ControllerFeature]
    [File("Controller")]
    public interface IPizzaController
    {
        [Get]
        Task PizzaList();
    }
}
