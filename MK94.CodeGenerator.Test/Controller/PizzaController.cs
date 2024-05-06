using MK94.CodeGenerator.Attributes;
using MK94.CodeGenerator.Intermediate.CSharp.Modules.StronglyTypedId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MK94.CodeGenerator.Intermediate.CSharp.Modules;
using MK94.CodeGenerator.Features;

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

[File("Data")]
public class PageResult<T>
{
    public int Total { get; set; }

    public List<T> Items { get; set; } = [];
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

[ControllerFeature]
[File("Controller")]
public interface IPizzaController
{
    [Get]
    Task<PageResult<Order>> PizzaList(Page page);

    [Post]
    Task Order([Body] Order order);
}
