namespace TestNameSpace;

public class Page
{
    public Guid PageId { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 
}
public interface IId
{
    public Guid Id { get; } 
}
public record struct PageId : IId
{
    public Guid Id { get; set; } 
}
