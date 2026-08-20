using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Pricing;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Seeding;
using Qalam.Service.Abstracts;
using System.Text.RegularExpressions;

namespace Qalam.Service.Implementations;

public class PricingAdminService : IPricingAdminService
{
    private static readonly Regex MarketCodePattern = new("^[a-z0-9]{2,10}$", RegexOptions.Compiled);

    private readonly IDomainSessionPriceRepository _domainSessionPriceRepository;
    private readonly IPricingMarketRepository _marketRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelUpgradeSuggestionRepository _suggestionRepository;
    private readonly IDomainRatePropagationService _propagationService;
    private readonly ApplicationDBContext _dbContext;

    public PricingAdminService(
        IDomainSessionPriceRepository domainSessionPriceRepository,
        IPricingMarketRepository marketRepository,
        IEducationDomainRepository domainRepository,
        ITeacherLevelRepository teacherLevelRepository,
        ITeacherRepository teacherRepository,
        ITeacherLevelUpgradeSuggestionRepository suggestionRepository,
        IDomainRatePropagationService propagationService,
        ApplicationDBContext dbContext)
    {
        _domainSessionPriceRepository = domainSessionPriceRepository;
        _marketRepository = marketRepository;
        _domainRepository = domainRepository;
        _teacherLevelRepository = teacherLevelRepository;
        _teacherRepository = teacherRepository;
        _suggestionRepository = suggestionRepository;
        _propagationService = propagationService;
        _dbContext = dbContext;
    }

    public async Task<List<PricingMarketDto>> ListPricingMarketsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _marketRepository.ListActiveAsync(cancellationToken);
        return rows.Select(MapPricingMarket).ToList();
    }

    public async Task<List<PricingMarketAdminDto>> ListPricingMarketsAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _marketRepository.ListAllAsync(cancellationToken);
        return rows.Select(MapPricingMarketAdmin).ToList();
    }

    public async Task<PricingMarketAdminDto> CreatePricingMarketAsync(
        CreatePricingMarketDto dto,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizeMarketCode(dto.Code);
        ValidateMarketCode(code);
        var currency = NormalizeCurrency(dto.Currency);

        if (await _marketRepository.ExistsAsync(code, cancellationToken))
            throw new InvalidOperationException($"Pricing market '{code}' already exists.");

        if (dto.IsDefault)
            await _marketRepository.ClearDefaultFlagAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var exchangeRate = code == PricingMarketDefaults.DefaultMarketCode
            ? 1m
            : dto.ExchangeRateFromBase is > 0 ? dto.ExchangeRateFromBase.Value : 1m;

        var market = new PricingMarket
        {
            Code = code,
            NameEn = dto.NameEn.Trim(),
            NameAr = dto.NameAr.Trim(),
            Currency = currency,
            IsActive = true,
            IsDefault = dto.IsDefault,
            ExchangeRateFromBase = exchangeRate,
            CreatedAt = now
        };

        await _marketRepository.AddAsync(market);
        await _marketRepository.SaveChangesAsync();

        await PricingMarketRateSeeder.SeedPlaceholderRatesForMarketAsync(
            _dbContext, code, now, cancellationToken);

        return MapPricingMarketAdmin(market);
    }

    public async Task<PricingMarketAdminDto?> UpdatePricingMarketAsync(
        string code,
        UpdatePricingMarketDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeMarketCode(code);
        var market = await _marketRepository.GetByCodeTrackedAsync(normalizedCode, cancellationToken);
        if (market == null)
            return null;

        if (!dto.IsActive && market.IsDefault)
            throw new InvalidOperationException("Cannot deactivate the platform default market. Set another market as default first.");

        market.NameEn = dto.NameEn.Trim();
        market.NameAr = dto.NameAr.Trim();
        market.Currency = NormalizeCurrency(dto.Currency);
        market.IsActive = dto.IsActive;
        market.UpdatedAt = DateTime.UtcNow;

        await _marketRepository.UpdateAsync(market);
        await _marketRepository.SaveChangesAsync();

        return MapPricingMarketAdmin(market);
    }

    public async Task<PricingMarketAdminDto?> SetDefaultPricingMarketAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeMarketCode(code);
        var market = await _marketRepository.GetByCodeTrackedAsync(normalizedCode, cancellationToken);
        if (market == null)
            return null;

        if (!market.IsActive)
            throw new InvalidOperationException("Cannot set an inactive market as the platform default.");

        await _marketRepository.ClearDefaultFlagAsync(cancellationToken);

        market.IsDefault = true;
        market.UpdatedAt = DateTime.UtcNow;
        await _marketRepository.UpdateAsync(market);
        await _marketRepository.SaveChangesAsync();

        return MapPricingMarketAdmin(market);
    }

    public async Task<List<DomainSessionPriceAdminDto>> ListDomainSessionPricesAsync(
        string marketCode,
        int? domainId,
        string? sessionTypeCode,
        bool includeHistory,
        CancellationToken cancellationToken = default)
    {
        var normalizedMarket = marketCode.Trim().ToLowerInvariant();
        if (!await _marketRepository.ExistsActiveAsync(normalizedMarket, cancellationToken))
            throw new InvalidOperationException($"Pricing market '{normalizedMarket}' is not available.");

        List<DomainSessionPrice> rows;

        if (includeHistory && domainId.HasValue && !string.IsNullOrWhiteSpace(sessionTypeCode))
        {
            rows = await _domainSessionPriceRepository.ListHistoryAsync(
                domainId.Value,
                sessionTypeCode.Trim(),
                normalizedMarket,
                cancellationToken);
        }
        else
        {
            rows = await _domainSessionPriceRepository.ListCurrentRatesAsync(normalizedMarket, cancellationToken);
            if (domainId.HasValue)
                rows = rows.Where(r => r.DomainId == domainId.Value).ToList();
            if (!string.IsNullOrWhiteSpace(sessionTypeCode))
                rows = rows.Where(r => r.SessionTypeCode == sessionTypeCode.Trim()).ToList();
        }

        var baseRateLookup = await BuildBaseRateLookupAsync(cancellationToken);
        return rows.Select(r => MapDomainSessionPrice(r, baseRateLookup)).ToList();
    }

    public async Task<DomainSessionPriceAdminDto?> SetDomainSessionPriceAsync(
        SetDomainSessionPriceDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.MarketCode))
        {
            var requestedMarket = dto.MarketCode.Trim().ToLowerInvariant();
            if (requestedMarket != PricingMarketDefaults.DefaultMarketCode)
                throw new InvalidOperationException("Domain rates must be set in the base market (SAR). Other markets are derived automatically.");
        }

        var domain = await _domainRepository.GetByIdAsync(dto.DomainId);
        if (domain == null)
            return null;

        var sessionTypeCode = dto.SessionTypeCode.Trim().ToLowerInvariant();
        if (sessionTypeCode is not ("individual" or "group"))
            throw new InvalidOperationException("SessionTypeCode must be 'individual' or 'group'.");

        var effectiveFrom = dto.EffectiveFrom?.ToUniversalTime() ?? DateTime.UtcNow;

        var current = await _domainSessionPriceRepository.GetCurrentRateAsync(
            dto.DomainId, sessionTypeCode, PricingMarketDefaults.DefaultMarketCode, cancellationToken);
        if (current != null && current.PricePerHour == dto.PricePerHour)
            return MapDomainSessionPrice(current, domain.Code, domain.NameEn, domain.NameAr);

        var baseRow = await _propagationService.PropagateBaseRateAsync(
            dto.DomainId,
            sessionTypeCode,
            dto.PricePerHour,
            effectiveFrom,
            cancellationToken);

        var market = await _marketRepository.GetByCodeAsync(PricingMarketDefaults.DefaultMarketCode, cancellationToken);
        baseRow.Market = market!;
        return MapDomainSessionPrice(baseRow, domain.Code, domain.NameEn, domain.NameAr);
    }

    public async Task<List<PricingExchangeRateAdminDto>> ListPricingExchangeRatesAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _marketRepository.ListAllAsync(cancellationToken);
        return rows.Select(MapExchangeRate).ToList();
    }

    public async Task<PricingExchangeRateAdminDto?> UpdatePricingExchangeRateAsync(
        string code,
        UpdatePricingExchangeRateDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeMarketCode(code);
        if (normalizedCode == PricingMarketDefaults.DefaultMarketCode)
            throw new InvalidOperationException("The base market exchange rate is fixed at 1 and cannot be changed.");

        if (dto.ExchangeRateFromBase <= 0)
            throw new InvalidOperationException("Exchange rate must be greater than zero.");

        var market = await _marketRepository.GetByCodeTrackedAsync(normalizedCode, cancellationToken);
        if (market == null)
            return null;

        market.ExchangeRateFromBase = dto.ExchangeRateFromBase;
        market.UpdatedAt = DateTime.UtcNow;
        await _marketRepository.UpdateAsync(market);
        await _marketRepository.SaveChangesAsync();

        await _propagationService.RecalculateMarketFromBaseAsync(
            normalizedCode,
            DateTime.UtcNow,
            cancellationToken);

        return MapExchangeRate(market);
    }

    private async Task<IReadOnlyDictionary<(int DomainId, string SessionTypeCode), decimal>> BuildBaseRateLookupAsync(
        CancellationToken cancellationToken)
    {
        var baseRates = await _domainSessionPriceRepository.ListCurrentRatesAsync(
            PricingMarketDefaults.DefaultMarketCode,
            cancellationToken);
        return baseRates.ToDictionary(
            r => (r.DomainId, r.SessionTypeCode),
            r => r.PricePerHour);
    }

    private List<DomainSessionPriceAdminDto> MapDomainSessionPrices(
        List<DomainSessionPrice> rows,
        IReadOnlyDictionary<(int DomainId, string SessionTypeCode), decimal> baseRateLookup)
        => rows.Select(r => MapDomainSessionPrice(r, baseRateLookup)).ToList();

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

    public async Task<BackfillStarterTeacherLevelsResultDto> BackfillStarterTeacherLevelsAsync(
        CancellationToken cancellationToken = default)
    {
        var updatedCount = await _teacherRepository.BackfillStarterLevelForTeachersWithoutLevelAsync(cancellationToken);
        return new BackfillStarterTeacherLevelsResultDto { UpdatedCount = updatedCount };
    }

    private static PricingMarketDto MapPricingMarket(PricingMarket m) =>
        new()
        {
            Code = m.Code,
            NameEn = m.NameEn,
            NameAr = m.NameAr,
            Currency = m.Currency
        };

    private static PricingMarketAdminDto MapPricingMarketAdmin(PricingMarket m) =>
        new()
        {
            Code = m.Code,
            NameEn = m.NameEn,
            NameAr = m.NameAr,
            Currency = m.Currency,
            IsActive = m.IsActive,
            IsDefault = m.IsDefault
        };

    private static string NormalizeMarketCode(string code) =>
        code.Trim().ToLowerInvariant();

    private static string NormalizeCurrency(string currency) =>
        currency.Trim().ToUpperInvariant();

    private static void ValidateMarketCode(string code)
    {
        if (!MarketCodePattern.IsMatch(code))
            throw new InvalidOperationException("Market code must be 2–10 lowercase letters or digits.");
    }

    private static DomainSessionPriceAdminDto MapDomainSessionPrice(DomainSessionPrice row) =>
        MapDomainSessionPrice(row, row.Domain?.Code, row.Domain?.NameEn, row.Domain?.NameAr);

    private static DomainSessionPriceAdminDto MapDomainSessionPrice(
        DomainSessionPrice row,
        IReadOnlyDictionary<(int DomainId, string SessionTypeCode), decimal> baseRateLookup) =>
        MapDomainSessionPrice(row, row.Domain?.Code, row.Domain?.NameEn, row.Domain?.NameAr, baseRateLookup);

    private static DomainSessionPriceAdminDto MapDomainSessionPrice(
        DomainSessionPrice row,
        string? domainCode,
        string? domainNameEn,
        string? domainNameAr,
        IReadOnlyDictionary<(int DomainId, string SessionTypeCode), decimal>? baseRateLookup = null)
    {
        var isBase = row.MarketCode == PricingMarketDefaults.DefaultMarketCode;
        decimal? basePrice = isBase
            ? row.PricePerHour
            : baseRateLookup != null
                && baseRateLookup.TryGetValue((row.DomainId, row.SessionTypeCode), out var bp)
                ? bp
                : null;

        return new DomainSessionPriceAdminDto
        {
            Id = row.Id,
            MarketCode = row.MarketCode,
            Currency = row.Market?.Currency,
            DomainId = row.DomainId,
            DomainCode = domainCode,
            DomainNameEn = domainNameEn,
            DomainNameAr = domainNameAr,
            SessionTypeCode = row.SessionTypeCode,
            PricePerHour = row.PricePerHour,
            BasePricePerHour = basePrice,
            IsDerived = !isBase,
            EffectiveFrom = row.EffectiveFrom,
            EffectiveTo = row.EffectiveTo,
            IsActive = row.IsActive,
            IsCurrent = row.EffectiveTo == null
        };
    }

    private static PricingExchangeRateAdminDto MapExchangeRate(PricingMarket m) =>
        new()
        {
            Code = m.Code,
            NameEn = m.NameEn,
            NameAr = m.NameAr,
            Currency = m.Currency,
            ExchangeRateFromBase = m.ExchangeRateFromBase,
            IsActive = m.IsActive
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
