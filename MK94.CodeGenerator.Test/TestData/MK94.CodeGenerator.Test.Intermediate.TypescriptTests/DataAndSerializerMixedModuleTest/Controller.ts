import { Page } from "./Data";
 import { Order } from "./Order";

 export interface IPizzaController {
}
export class PizzaControllerApi {

    static async PizzaList(f = fetch, page: Page, init?: RequestInit): Promise<PageResult<Order>> {
        const _params: Record<string, string> = {};
        
        if (page?.pageId !== undefined && page?.pageId !== null) _params["PageId"] = page?.pageId.toString();
        if (page?.size !== undefined && page?.size !== null) _params["Size"] = page?.size.toString();
        if (page?.index !== undefined && page?.index !== null) _params["Index"] = page?.index.toString();
        
        const ret = await f("Pizza/PizzaList?" + new URLSearchParams(_params).toString(), init);
        return ret.json();
    }

    static async Order(f = fetch, order: Order, init?: RequestInit): Promise<void> {
        init = {
            ...init,
            method: "POST",
            headers: {...init?.headers, "Content-Type": "application/json" },
            body: JSON.stringify(order),
        };
        
        const ret = await f("Pizza/Order", init);
        return ret.json();
    }
}
