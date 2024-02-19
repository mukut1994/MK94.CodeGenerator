using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PizzaController
{
    public static Task PizzaList(Int32 page, Int32 count)
    {
        return "PizzaList"
          .SetQueryParam("page", page)
          .SetQueryParam("count", count)
          .ReceiveStringAsync();
    }
}
