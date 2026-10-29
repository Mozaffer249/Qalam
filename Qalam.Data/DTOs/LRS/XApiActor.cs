using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiActor
{
    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = "Agent";

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("mbox")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mbox { get; set; }

    [JsonPropertyName("account")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiAccount? Account { get; set; }
}

public class XApiAccount
{
    [JsonPropertyName("homePage")]
    public string HomePage { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
