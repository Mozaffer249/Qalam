using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StudentIdentityService : IStudentIdentityService
{
    private const string SettingKey = "StudentIdentityRules";
    private const string CacheKey = "StudentIdentitySettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static readonly StudentIdentitySettings DefaultSettings = new()
    {
        Rules = new List<StudentIdentityRule>
        {
            new() { NationalityCode = "SA", AllowedIdentityTypes = new List<int> { (int)IdentityType.NationalId }, IssuingCountryRequired = false },
            new() { NationalityCode = "*", AllowedIdentityTypes = new List<int> { (int)IdentityType.Iqama, (int)IdentityType.Passport }, IssuingCountryRequired = true },
        },
        IdentityRequiredForEnrollment = false
    };

    private readonly ISystemSettingRepository _settings;
    private readonly INationalityRepository _nationalities;
    private readonly IMemoryCache _cache;

    public StudentIdentityService(
        ISystemSettingRepository settings,
        INationalityRepository nationalities,
        IMemoryCache cache)
    {
        _settings = settings;
        _nationalities = nationalities;
        _cache = cache;
    }

    public async Task<StudentIdentitySettings> GetSettingsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out StudentIdentitySettings? cached) && cached != null)
            return cached;

        var setting = await _settings.GetByKeyAsync(SettingKey, ct);
        StudentIdentitySettings result;
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            try
            {
                result = JsonSerializer.Deserialize<StudentIdentitySettings>(setting.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? DefaultSettings;
            }
            catch
            {
                result = DefaultSettings;
            }
        }
        else
        {
            result = DefaultSettings;
        }

        _cache.Set(CacheKey, result, CacheDuration);
        return result;
    }

    public async Task<StudentIdentityRule?> GetRuleForNationalityAsync(int nationalityId, CancellationToken ct = default)
    {
        var nationality = await _nationalities.GetByIdAsync(nationalityId);
        if (nationality == null || !nationality.IsActive) return null;

        var settings = await GetSettingsAsync(ct);
        var rule = settings.Rules.FirstOrDefault(r =>
            r.NationalityCode.Equals(nationality.Code, StringComparison.OrdinalIgnoreCase));

        return rule ?? settings.Rules.FirstOrDefault(r => r.NationalityCode == "*");
    }

    public async Task<List<IdentityType>> GetAllowedIdentityTypesAsync(int nationalityId, CancellationToken ct = default)
    {
        var rule = await GetRuleForNationalityAsync(nationalityId, ct);
        if (rule == null) return new List<IdentityType>();

        return rule.AllowedIdentityTypes
            .Where(v => Enum.IsDefined(typeof(IdentityType), v))
            .Select(v => (IdentityType)v)
            .ToList();
    }
}
