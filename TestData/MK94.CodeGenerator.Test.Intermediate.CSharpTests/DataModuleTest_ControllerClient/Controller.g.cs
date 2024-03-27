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
}
public class PizzaControllerClient(FlurlClient client)
{

    public Task PizzaList()
    {
        await client.Request("/api/Pizza/PizzaList");
    }
}
