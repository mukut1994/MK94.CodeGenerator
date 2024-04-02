using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;

namespace MK94.CodeGenerator.Test.Controller;

[File("Data")]
[StronglyTypedId]
public struct PageId { }

[File("Data")]
public class Page
{
    [Query]
    public PageId PageId { get; set; }

    [Query]
    public int Size { get; set; }

    [Query]
    public int Index { get; set; }
}

[File("Pizza")]
public enum PizzaType
{
    DoughOnly,
    Pineapple,
}

[File("Order")]
public class Order
{
    public PizzaType? PizzaType { get; set; }
}

[File("Pizza")]
public class Pizza
{
    public string Name { get; set; }
}

[ControllerFeature]
[File("Controller")]
public interface IPizzaController
{
    [Get]
    Task PizzaList([Query] Page page);

    [Get]
    Task<Pizza> Get([Query] Guid id);

    [Post]
    Task Order([Body] Order order);
}
