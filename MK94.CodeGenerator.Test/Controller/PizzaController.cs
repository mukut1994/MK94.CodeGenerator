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

[ControllersProject]
[ControllerFeature]
[File("Controller")]
public interface IPizzaController
{
    [Get]
    Task PizzaList([Query] Page page);
}
