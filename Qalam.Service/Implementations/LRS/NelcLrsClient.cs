using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations.LRS;

public class NelcLrsClient : ILrsClient
{
    private readonly HttpClient _http;
    private readonly LrsSettings _settings;
    private readonly ILogger<NelcLrsClient> _logger;

    public NelcLrsClient(HttpClient http, IOptions<LrsSettings> settings, ILogger<NelcLrsClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.Endpoint))
            _http.BaseAddress = new Uri(_settings.Endpoint);

        if (!string.IsNullOrEmpty(_settings.Key) && !string.IsNullOrEmpty(_settings.Secret))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.Key}:{_settings.Secret}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        _http.DefaultRequestHeaders.Add("X-Experience-API-Version", _settings.XApiVersion);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LrsSendResult> SendStatementAsync(string statementJson, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(statementJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("", content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("LRS statement sent successfully. Status={StatusCode}", (int)response.StatusCode);
                return new LrsSendResult(true, (int)response.StatusCode, null);
            }

            _logger.LogWarning(
                "LRS statement rejected. Status={StatusCode}, Body={ResponseBody}",
                (int)response.StatusCode, responseBody);
            return new LrsSendResult(false, (int)response.StatusCode, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LRS HTTP request failed");
            return new LrsSendResult(false, null, ex.Message);
        }
    }
}
