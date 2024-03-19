using System.Text.Json.Serialization;
using System.Text.Json;

namespace TestNameSpace;

public class Page
{
    public Guid PageId2 { get; set; } 
    public Int32 Size { get; set; } 
    public Int32 Index { get; set; } 
}
public interface GuidId
{
    public Guid Id { get; } 
}
[JsonConverter(typeof(PageId2Converter))]
public record struct PageId2(Guid Id): GuidId
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
public class PageId2Converter : JsonConverter<PageId2>
{
    public override PageId2 Read(Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new PageId2(Guid.Parse(reader.GetString()!));
    }
    public override void Write(Utf8JsonWriter writer, PageId2 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
public partial class PageId2EfCoreValueConverter : global::Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<PageId2, global::System.Guid>
{
    public PageId2EfCoreValueConverter(global::Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints? mappingHints = null): base(id => id.Id, value => new PageId2(value), mappingHints)
    {
        
    }
}
