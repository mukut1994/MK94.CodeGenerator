using Microsoft.AspNetCore.Mvc;

namespace TestNameSpace;

[Route(api/[controller]/[action])]
public class PizzaController
{
    [HttpGet]
    public partial Task PizzaList([FromQuery]Page page);[HttpPost]
    public partial Task Order([FromBody]Order order);
}
