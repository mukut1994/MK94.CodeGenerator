using System.Threading.Task;
using Flurl;

namespace TestNameSpace;

public class Page
{
    public Guid PageId { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 

    public override String ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}
