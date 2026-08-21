using Qalam.Data.DTOs.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingAdminService
{
    Task<List<PricingMarketDto>> ListPricingMarketsAsync(CancellationToken cancellationToken = default);

    Task<List<PricingMarketAdminDto>> ListPricingMarketsAdminAsync(CancellationToken cancellationToken = default);

    Task<PricingMarketAdminDto> CreatePricingMarketAsync(
        CreatePricingMarketDto dto,
        CancellationToken cancellationToken = default);

    Task<PricingMarketAdminDto?> UpdatePricingMarketAsync(
        string code,
        UpdatePricingMarketDto dto,
        CancellationToken cancellationToken = default);

    Task<PricingMarketAdminDto?> SetDefaultPricingMarketAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<List<DomainSessionPriceAdminDto>> ListDomainSessionPricesAsync(
        string marketCode,
        int? domainId,
        string? sessionTypeCode,
        bool includeHistory,
        CancellationToken cancellationToken = default);

    Task<DomainSessionPriceAdminDto?> SetDomainSessionPriceAsync(
        SetDomainSessionPriceDto dto,
        CancellationToken cancellationToken = default);

    Task<List<PricingExchangeRateAdminDto>> ListPricingExchangeRatesAsync(
        CancellationToken cancellationToken = default);

    Task<PricingExchangeRateAdminDto?> UpdatePricingExchangeRateAsync(
        string code,
        UpdatePricingExchangeRateDto dto,
        CancellationToken cancellationToken = default);

    Task<List<TeacherLevelTierAdminDto>> ListTeacherLevelTiersAsync(
        CancellationToken cancellationToken = default);

    Task<TeacherLevelTierAdminDto?> SetTeacherLevelTierAsync(
        int id,
        SetTeacherLevelTierDto dto,
        CancellationToken cancellationToken = default);

    Task<TeacherLevelTierAdminDto> CreateTeacherLevelTierAsync(
        CreateTeacherLevelTierDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> SetTeacherLevelAsync(
        int teacherId,
        SetTeacherLevelDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> SetTeacherShareOverrideAsync(
        int teacherId,
        SetTeacherShareOverrideDto dto,
        CancellationToken cancellationToken = default);

    Task<List<TeacherLevelUpgradeSuggestionAdminDto>> ListLevelUpgradeSuggestionsAsync(
        string status,
        CancellationToken cancellationToken = default);

    Task<bool> ApproveLevelUpgradeSuggestionAsync(
        int id,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<bool> RejectLevelUpgradeSuggestionAsync(
        int id,
        string? reviewNotes,
        CancellationToken cancellationToken = default);

    Task<BackfillStarterTeacherLevelsResultDto> BackfillStarterTeacherLevelsAsync(
        CancellationToken cancellationToken = default);

    Task<FreeSessionPolicyStatsDto> GetFreeSessionPolicyStatsAsync(
        CancellationToken cancellationToken = default);
}
