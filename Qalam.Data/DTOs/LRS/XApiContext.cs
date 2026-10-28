using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiContext
{
    [JsonPropertyName("platform")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Platform { get; set; }

    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("instructor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiActor? Instructor { get; set; }

    [JsonPropertyName("contextActivities")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiContextActivities? ContextActivities { get; set; }

    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Extensions { get; set; }
}

public class XApiContextActivities
{
    [JsonPropertyName("parent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<XApiObject>? Parent { get; set; }

    [JsonPropertyName("grouping")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<XApiObject>? Grouping { get; set; }
}
