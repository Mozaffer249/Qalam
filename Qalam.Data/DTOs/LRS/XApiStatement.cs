using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiStatement
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("actor")]
    public XApiActor Actor { get; set; } = new();

    [JsonPropertyName("verb")]
    public XApiVerb Verb { get; set; } = new();

    [JsonPropertyName("object")]
    public XApiObject Object { get; set; } = new();

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiResult? Result { get; set; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public XApiContext? Context { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}
