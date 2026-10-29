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
    public const string Course = "https://w3id.org/xapi/cmi5/activitytype/course";
    public const string VirtualClassroom = "https://w3id.org/xapi/virtual-classroom/activity-types/virtual-classroom";
    public const string Module = "http://adlnet.gov/expapi/activities/module";
    public const string Lesson = "http://adlnet.gov/expapi/activities/lesson";
    public const string Certificate = "https://www.opigno.org/en/tincan_registry/activity_type/certificate";
}
