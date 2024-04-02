import { Page } from "./Data";
 import { Order } from "./Order";

 export class PizzaControllerApi {

    static async PizzaList(f = fetch, init: RequestInit, page: Page): Promise<void> {
        const ret = await f("PizzaController/PizzaList", init)
        return ret.json();
    }

    static async Order(f = fetch, init: RequestInit, order: Order): Promise<void> {
        init = {
            ...init,
            body: JSON.stringify(order),
        };
        
        const ret = await f("PizzaController/Order", init)
        return ret.json();
    }
}
