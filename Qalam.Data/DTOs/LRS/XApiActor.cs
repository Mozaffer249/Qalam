using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiActor
{
    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = "Agent";

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("account")]
    public XApiAccount Account { get; set; } = new();
}

public class XApiAccount
{
    [JsonPropertyName("homePage")]
    public string HomePage { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
