using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PizzaController
{
    public static Task PizzaList(Page page)
    {
        return "PizzaList"
          .SetQueryParam("page", page)
          .ReceiveStringAsync();
    }
}
