namespace TestNameSpace;

public class PageResult<T>
{
    public Int32 Total { get; set; } 
    public List<T> Items { get; set; } 
}
public class Page
{
    public PageId PageId { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 
}
public class PageId
{
}
