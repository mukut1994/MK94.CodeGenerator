namespace TestNameSpace;

public class Pizza
{
    public String Name { get; set; } 

    public override String ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
