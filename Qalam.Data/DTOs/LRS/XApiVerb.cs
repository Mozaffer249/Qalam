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
    public const string Initialized = "http://adlnet.gov/expapi/verbs/initialized";
    public const string Attended = "http://adlnet.gov/expapi/verbs/attended";
    public const string Completed = "http://adlnet.gov/expapi/verbs/completed";
    public const string Progressed = "http://adlnet.gov/expapi/verbs/progressed";
    public const string Rated = "http://id.tincanapi.com/verb/rated";
    public const string Earned = "http://id.tincanapi.com/verb/earned";

    public static XApiVerb Create(string iri, string enLabel)
    {
        return new XApiVerb
        {
            Id = iri,
            Display = new Dictionary<string, string> { ["en-US"] = enLabel }
        };
    }
}
