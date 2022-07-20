using MK94.DataGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Test.Controller
{
    [ControllerFeature]
    [File("Controller")]
    public interface IPizzaController
    {
        [Get]
        Task PizzaList();
    }
}
