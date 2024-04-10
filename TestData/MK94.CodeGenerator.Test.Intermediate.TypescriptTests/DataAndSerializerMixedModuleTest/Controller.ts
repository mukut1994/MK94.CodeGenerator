import { Page } from "./Data";
 import { Order } from "./Order";

 export interface IPizzaController {
}
export class PizzaControllerApi {

    static async PizzaList(f = fetch, page: Page, init?: RequestInit): Promise<void> {
        const _params: Record<string, string> = {};
        
        if (page?.pageId !== undefined && page?.pageId !== null) _params["PageId"] = page?.pageId.toString();
        if (page?.size !== undefined && page?.size !== null) _params["Size"] = page?.size.toString();
        if (page?.index !== undefined && page?.index !== null) _params["Index"] = page?.index.toString();
        
        init = {
            ...init,
            method: "POST",
        };
        
        const ret = await f("Pizza/PizzaList?" + new URLSearchParams(_params).toString(), init);
        return ret.json();
    }

    static async Get(f = fetch, id: string, init?: RequestInit): Promise<Pizza> {
        const _params: Record<string, string> = {};
        
        if (id?.id !== undefined && id?.id !== null) _params["id"] = id?.id.toString();
        
        init = {
            ...init,
            method: "POST",
        };
        
        const ret = await f("Pizza/Get?" + new URLSearchParams(_params).toString(), init);
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
