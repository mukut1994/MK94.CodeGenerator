using using System.Text;;
using using System.Linq;;
using using System.IO;;
using using System.Collections.Generic;;
using using Flurl.Http;;
using using Flurl;;
using System;
using Microsoft.AspNetCore.Mvc;

namespace TestNameSpace;

[Route("api/[controller]/[action]")]
public class PizzaController
{
    [HttpGet]
    public partial Task<List<Pizza>> PizzaList([FromQuery]Page page);
    [HttpPost]
    public partial Task Order([FromBody]Order order);
}
public class PizzaControllerClient(FlurlClient client)
{

    public async Task<List<Pizza>> PizzaList()
    {
        return await client.Request("/api/Pizza/PizzaList").GetJsonAsync<List`1>();
    }
    public async Task Order()
    {
        await client.Request("/api/Pizza/Order").PostJsonAsync();
    }
}
