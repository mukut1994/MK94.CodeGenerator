export class PizzaControllerApi
{
    static async PizzaList(page: Page, f = fetch): Promise<void>
    {
        const ret = await f("api/v1/PizzaController/PizzaList")
        return ret.json();
    }
}
