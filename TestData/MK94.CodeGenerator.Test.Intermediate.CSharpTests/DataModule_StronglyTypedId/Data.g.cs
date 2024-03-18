using System.Text.Json.Serialization;
using System.Text.Json;

namespace TestNameSpace;

public class Page
{
    public Guid PageId { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 
}
public interface GuidId
{
    public Guid Id { get; } 
}
[JsonConverter(typeof(PageIdConverter))]
public record struct PageId(Guid Id): GuidId
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
public class PageIdConverter : JsonConverter<PageId>
{
    public override PageId Read(Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new PageId(Guid.Parse(reader.GetString()!));
    }
    public override void Write(Utf8JsonWriter writer, PageId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
