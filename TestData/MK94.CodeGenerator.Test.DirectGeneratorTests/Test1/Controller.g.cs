using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace space
{
    public class PizzaController
    {
        private readonly FlurlClient client;
        public PizzaController(FlurlClient client) { this.client = client; }

        public async Task PizzaList(Page page)
        {
            await client.Request("/api/Pizza/PizzaList".SetQueryParam("page", page)).GetAsync();
        }

    }
}
