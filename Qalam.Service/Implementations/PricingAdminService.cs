using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class PricingAdminService : IPricingAdminService
{
    private readonly IDomainSessionPriceRepository _domainSessionPriceRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelUpgradeSuggestionRepository _suggestionRepository;

    public PricingAdminService(
        IDomainSessionPriceRepository domainSessionPriceRepository,
        IEducationDomainRepository domainRepository,
        ITeacherLevelRepository teacherLevelRepository,
        ITeacherRepository teacherRepository,
        ITeacherLevelUpgradeSuggestionRepository suggestionRepository)
    {
        _domainSessionPriceRepository = domainSessionPriceRepository;
        _domainRepository = domainRepository;
        _teacherLevelRepository = teacherLevelRepository;
        _teacherRepository = teacherRepository;
        _suggestionRepository = suggestionRepository;
    }

    public async Task<List<DomainSessionPriceAdminDto>> ListDomainSessionPricesAsync(
        int? domainId,
        string? sessionTypeCode,
        bool includeHistory,
        CancellationToken cancellationToken = default)
    {
        List<DomainSessionPrice> rows;

        if (includeHistory && domainId.HasValue && !string.IsNullOrWhiteSpace(sessionTypeCode))
        {
            rows = await _domainSessionPriceRepository.ListHistoryAsync(
                domainId.Value,
                sessionTypeCode.Trim(),
                cancellationToken);
        }
        else
        {
            rows = await _domainSessionPriceRepository.ListCurrentRatesAsync(cancellationToken);
            if (domainId.HasValue)
                rows = rows.Where(r => r.DomainId == domainId.Value).ToList();
            if (!string.IsNullOrWhiteSpace(sessionTypeCode))
                rows = rows.Where(r => r.SessionTypeCode == sessionTypeCode.Trim()).ToList();
        }

        return rows.Select(MapDomainSessionPrice).ToList();
    }

    public async Task<DomainSessionPriceAdminDto?> SetDomainSessionPriceAsync(
        SetDomainSessionPriceDto dto,
        CancellationToken cancellationToken = default)
    {
        var domain = await _domainRepository.GetByIdAsync(dto.DomainId);
        if (domain == null)
            return null;

        var sessionTypeCode = dto.SessionTypeCode.Trim().ToLowerInvariant();
        if (sessionTypeCode is not ("individual" or "group"))
            throw new InvalidOperationException("SessionTypeCode must be 'individual' or 'group'.");

        var effectiveFrom = dto.EffectiveFrom?.ToUniversalTime() ?? DateTime.UtcNow;

        var current = await _domainSessionPriceRepository.GetCurrentRateAsync(
            dto.DomainId, sessionTypeCode, cancellationToken);
        if (current != null && current.PricePerHour == dto.PricePerHour)
            return MapDomainSessionPrice(current, domain.Code, domain.NameEn, domain.NameAr);

        await _domainSessionPriceRepository.CloseCurrentRateAsync(
            dto.DomainId, sessionTypeCode, effectiveFrom, cancellationToken);

        var row = new DomainSessionPrice
        {
            DomainId = dto.DomainId,
            SessionTypeCode = sessionTypeCode,
            PricePerHour = dto.PricePerHour,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _domainSessionPriceRepository.AddAsync(row);
        await _domainSessionPriceRepository.SaveChangesAsync();

        return MapDomainSessionPrice(row, domain.Code, domain.NameEn, domain.NameAr);
    }

    public async Task<List<TeacherLevelTierAdminDto>> ListTeacherLevelTiersAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _teacherLevelRepository.ListOrderedAsync(cancellationToken);
        return rows.Select(MapTeacherLevelTier).ToList();
    }

    public async Task<TeacherLevelTierAdminDto?> SetTeacherLevelTierAsync(
        int id,
        SetTeacherLevelTierDto dto,
        CancellationToken cancellationToken = default)
    {
        var level = await _teacherLevelRepository.GetByIdAsync(id);
        if (level == null)
            return null;

        level.TeacherSharePct = dto.TeacherSharePct;
        if (!string.IsNullOrWhiteSpace(dto.NameAr))
            level.NameAr = dto.NameAr.Trim();
        if (!string.IsNullOrWhiteSpace(dto.NameEn))
            level.NameEn = dto.NameEn.Trim();
        if (dto.IsActive.HasValue)
            level.IsActive = dto.IsActive.Value;
        level.UpdatedAt = DateTime.UtcNow;

        await _teacherLevelRepository.UpdateAsync(level);
        await _teacherLevelRepository.SaveChangesAsync();

        return MapTeacherLevelTier(level);
    }

    public async Task<bool> SetTeacherLevelAsync(
        int teacherId,
        SetTeacherLevelDto dto,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return false;

        var level = await _teacherLevelRepository.GetByIdAsync(dto.TeacherLevelId);
        if (level == null || !level.IsActive)
            throw new InvalidOperationException("Teacher level not found or inactive.");

        teacher.TeacherLevelId = level.Id;
        teacher.UpdatedAt = DateTime.UtcNow;
        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetTeacherShareOverrideAsync(
        int teacherId,
        SetTeacherShareOverrideDto dto,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return false;

        teacher.CustomTeacherSharePct = dto.CustomTeacherSharePct;
        teacher.UpdatedAt = DateTime.UtcNow;
        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        return true;
    }

    public async Task<List<TeacherLevelUpgradeSuggestionAdminDto>> ListLevelUpgradeSuggestionsAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<TeacherLevelUpgradeSuggestionStatus>(status, true, out var parsedStatus))
            throw new InvalidOperationException("Invalid status filter.");

        var rows = await _suggestionRepository.ListByStatusAsync(parsedStatus, cancellationToken);
        return rows.Select(MapUpgradeSuggestion).ToList();
    }

    public async Task<bool> ApproveLevelUpgradeSuggestionAsync(
        int id,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id);
        if (suggestion == null)
            return false;
        if (suggestion.Status != TeacherLevelUpgradeSuggestionStatus.Pending)
            throw new InvalidOperationException("Suggestion is not pending.");

        var teacher = await _teacherRepository.GetByIdAsync(suggestion.TeacherId);
        if (teacher == null)
            return false;

        teacher.TeacherLevelId = suggestion.SuggestedLevelId;
        teacher.UpdatedAt = DateTime.UtcNow;
        suggestion.Status = TeacherLevelUpgradeSuggestionStatus.Approved;
        suggestion.ReviewedAt = DateTime.UtcNow;
        suggestion.ReviewNotes = reviewNotes;

        await _teacherRepository.UpdateAsync(teacher);
        await _suggestionRepository.UpdateAsync(suggestion);
        await _suggestionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectLevelUpgradeSuggestionAsync(
        int id,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(id);
        if (suggestion == null)
            return false;
        if (suggestion.Status != TeacherLevelUpgradeSuggestionStatus.Pending)
            throw new InvalidOperationException("Suggestion is not pending.");

        suggestion.Status = TeacherLevelUpgradeSuggestionStatus.Rejected;
        suggestion.ReviewedAt = DateTime.UtcNow;
        suggestion.ReviewNotes = reviewNotes;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _suggestionRepository.UpdateAsync(suggestion);
        await _suggestionRepository.SaveChangesAsync();

        return true;
    }

    private static DomainSessionPriceAdminDto MapDomainSessionPrice(DomainSessionPrice row) =>
        MapDomainSessionPrice(row, row.Domain?.Code, row.Domain?.NameEn, row.Domain?.NameAr);

    private static DomainSessionPriceAdminDto MapDomainSessionPrice(
        DomainSessionPrice row,
        string? domainCode,
        string? domainNameEn,
        string? domainNameAr) =>
        new()
        {
            Id = row.Id,
            DomainId = row.DomainId,
            DomainCode = domainCode,
            DomainNameEn = domainNameEn,
            DomainNameAr = domainNameAr,
            SessionTypeCode = row.SessionTypeCode,
            PricePerHour = row.PricePerHour,
            EffectiveFrom = row.EffectiveFrom,
            EffectiveTo = row.EffectiveTo,
            IsActive = row.IsActive,
            IsCurrent = row.EffectiveTo == null
        };

    private static TeacherLevelTierAdminDto MapTeacherLevelTier(Data.Entity.Teacher.TeacherLevel level) =>
        new()
        {
            Id = level.Id,
            Code = level.Code,
            NameAr = level.NameAr,
            NameEn = level.NameEn,
            OrderIndex = level.OrderIndex,
            TeacherSharePct = level.TeacherSharePct,
            IsActive = level.IsActive
        };

    private static TeacherLevelUpgradeSuggestionAdminDto MapUpgradeSuggestion(
        Data.Entity.Teacher.TeacherLevelUpgradeSuggestion suggestion) =>
        new()
        {
            Id = suggestion.Id,
            TeacherId = suggestion.TeacherId,
            TeacherName = string.Join(" ",
                new[] { suggestion.Teacher.User?.FirstName, suggestion.Teacher.User?.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))),
            CurrentLevelId = suggestion.CurrentLevelId,
            CurrentLevelCode = suggestion.CurrentLevel.Code,
            SuggestedLevelId = suggestion.SuggestedLevelId,
            SuggestedLevelCode = suggestion.SuggestedLevel.Code,
            AvgRating = suggestion.AvgRating,
            CompletedSessions = suggestion.CompletedSessions,
            AttendanceRate = suggestion.AttendanceRate,
            Status = suggestion.Status.ToString(),
            CreatedAt = suggestion.CreatedAt
        };
}
