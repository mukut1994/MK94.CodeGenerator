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
public record struct PageId(Guid Id): IId
{

    public static Guid Empty()
    {
        return new(Guid.Empty);
    }
    public static Guid New()
    {
        return new(Guid.NewGuid());
    }
    public override String ToString()
    {
        return Id.ToString();
    }
}
