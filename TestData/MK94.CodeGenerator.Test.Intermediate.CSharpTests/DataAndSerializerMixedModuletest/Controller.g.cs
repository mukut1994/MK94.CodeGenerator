using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PizzaController
{
    public static Task PizzaList(Page page)
    {
        return "PizzaList"
          .SetQueryParam("pageId", page.PageId)
          .SetQueryParam("size", page.Size)
          .SetQueryParam("index", page.Index)
          .ReceiveStringAsync();
    }
    public static Task Order(Order order)
    {
        return "Order"
          .ReceiveStringAsync();
    }
}
