using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using Flurl.Http;
using Flurl;

namespace TestNameSpace;

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
