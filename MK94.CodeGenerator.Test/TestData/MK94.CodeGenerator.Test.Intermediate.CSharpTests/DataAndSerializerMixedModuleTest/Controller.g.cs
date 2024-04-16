using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PizzaController
{
    public static Task PizzaList(Page page)
    {
        return "PizzaList"
          .ReceiveStringAsync();
    }
    public static Task Order(Order order)
    {
        return "Order"
          .ReceiveStringAsync();
    }
}
