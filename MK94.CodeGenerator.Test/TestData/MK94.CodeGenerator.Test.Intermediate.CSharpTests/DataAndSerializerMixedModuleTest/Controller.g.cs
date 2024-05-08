using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class IPizzaController
{
    public static Task<PageResult<Order>> PizzaList(Page page)
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
