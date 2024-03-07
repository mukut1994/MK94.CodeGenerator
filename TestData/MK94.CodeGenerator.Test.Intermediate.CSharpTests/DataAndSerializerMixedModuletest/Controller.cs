using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PizzaController
{
    public static Task PizzaList(Page page)
    {
        return "PizzaList"
          .SetQueryParam("size", page.Size)
          .SetQueryParam("index", page.Index)
          .ReceiveStringAsync();
    }
}
