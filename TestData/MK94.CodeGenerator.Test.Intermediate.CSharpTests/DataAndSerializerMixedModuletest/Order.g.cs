namespace TestNameSpace;

public class Order
{
    public PizzaType PizzaType { get; set; } 

    public override String ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
