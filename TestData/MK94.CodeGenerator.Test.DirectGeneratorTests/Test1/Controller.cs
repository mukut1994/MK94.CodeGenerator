using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Flurl;
using Flurl.Http;
using System.Threading.Tasks;

namespace space
{
    public class PizzaController
    {
        private readonly FlurlClient client;
        public PizzaController(FlurlClient client) { this.client = client; }

        public async Task PizzaList()
        {
            await client.Request("/api/Pizza/PizzaList").GetAsync();
        }

    }
}
