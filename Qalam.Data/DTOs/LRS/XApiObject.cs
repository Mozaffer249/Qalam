using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiObject
{
    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = "Activity";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("definition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiActivityDefinition? Definition { get; set; }
}

public class XApiActivityDefinition
{
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Name { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Description { get; set; }
}

public static class XApiActivityTypes
{
    public const string Course = "http://adlnet.gov/expapi/activities/course";
    public const string Session = "http://adlnet.gov/expapi/activities/session";
    public const string Module = "http://adlnet.gov/expapi/activities/module";
}
