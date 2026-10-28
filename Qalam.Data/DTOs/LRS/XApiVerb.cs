using System.Text.Json.Serialization;

namespace Qalam.Data.DTOs.LRS;

public class XApiVerb
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public Dictionary<string, string> Display { get; set; } = new();
}

public static class XApiVerbs
{
    public const string Registered = "http://adlnet.gov/expapi/verbs/registered";
    public const string Launched = "http://adlnet.gov/expapi/verbs/launched";
    public const string Attended = "http://adlnet.gov/expapi/verbs/attended";
    public const string Completed = "http://adlnet.gov/expapi/verbs/completed";
    public const string Progressed = "http://adlnet.gov/expapi/verbs/progressed";

    public static XApiVerb Create(string iri, string enLabel, string? arLabel = null)
    {
        var verb = new XApiVerb
        {
            Id = iri,
            Display = new Dictionary<string, string> { ["en-US"] = enLabel }
        };
        if (arLabel != null)
            verb.Display["ar"] = arLabel;
        return verb;
    }
}
