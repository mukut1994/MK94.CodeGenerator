namespace TestNameSpace;

public interface IPizzaManager
{
    public Task<PageResult<Order>> PizzaList(Page page);
    public Task Order(Order order);
}
