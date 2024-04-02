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
    public partial Task PizzaList([FromQuery]Page page);
    [HttpGet]
    public partial Task<Pizza> Get([FromQuery]Guid id);
    [HttpPost]
    public partial Task Order([FromBody]Order order);
}
public class PizzaControllerClient(FlurlClient client)
{

    public async Task PizzaList()
    {
        await client.Request("/api/Pizza/PizzaList").GetAsync();
    }
    public async Task<Pizza> Get()
    {
        return await client.Request("/api/Pizza/Get").GetJsonAsync<Pizza>();
    }
    public async Task Order()
    {
        await client.Request("/api/Pizza/Order").PostJsonAsync();
    }
}
