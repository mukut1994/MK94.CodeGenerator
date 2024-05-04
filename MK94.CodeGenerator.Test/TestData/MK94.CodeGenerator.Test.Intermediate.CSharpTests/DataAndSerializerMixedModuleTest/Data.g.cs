using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class PageResult<T>
{
    public Int32 Total { get; set; } 
    public List<T> Items { get; set; } 

    public override String ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
public class Page
{
    public PageId PageId { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 

    public override String ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
public class PageId
{
}
