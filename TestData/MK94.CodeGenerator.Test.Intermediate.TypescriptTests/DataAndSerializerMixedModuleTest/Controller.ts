export class PizzaControllerApi
{
    static async PizzaList(page: TODO, f = fetch): TODO
    {
        const ret = await f("api/v1/PizzaController/PizzaList")
        return ret.json();
    }
}
