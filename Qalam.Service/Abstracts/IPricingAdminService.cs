using Qalam.Data.DTOs.Pricing;

namespace Qalam.Service.Abstracts;

public interface IPricingAdminService
{
    Task<List<PricingMarketDto>> ListPricingMarketsAsync(CancellationToken cancellationToken = default);

    Task<List<DomainSessionPriceAdminDto>> ListDomainSessionPricesAsync(
        string marketCode,
        int? domainId,
        string? sessionTypeCode,
        bool includeHistory,
        CancellationToken cancellationToken = default);

    Task<DomainSessionPriceAdminDto?> SetDomainSessionPriceAsync(
        SetDomainSessionPriceDto dto,
        CancellationToken cancellationToken = default);

    Task<List<TeacherLevelTierAdminDto>> ListTeacherLevelTiersAsync(
        CancellationToken cancellationToken = default);

    Task<TeacherLevelTierAdminDto?> SetTeacherLevelTierAsync(
        int id,
        SetTeacherLevelTierDto dto,
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
}
