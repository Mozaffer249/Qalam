using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teaching;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

/// <summary>
/// One-shot remediation: merge duplicate domains into the approved (older / custom-Q) row,
/// remapping catalog FKs and archiving the newer twin. Wave-1 / sharia without a twin are kept.
/// </summary>
public static class EducationDomainDuplicateRemediationSeeder
{
    private static readonly HashSet<string> CanonicalCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        EducationDomainCodes.School,
        EducationDomainCodes.Quran,
        EducationDomainCodes.Language,
        EducationDomainCodes.Skills,
        EducationDomainCodes.University,
        EducationDomainCodes.SoftSkills,
        EducationDomainCodes.LifeSkills,
        EducationDomainCodes.TechSkills,
        EducationDomainCodes.Hobbies,
        EducationDomainCodes.Finance,
        EducationDomainCodes.Knowledge,
        EducationDomainCodes.Sharia,
    };

    /// <summary>
    /// Prod-shaped pairs from txfiles: keep the legacy-code row (custom questions),
    /// donate the canonical Excel code from the seeder twin.
    /// </summary>
    private static readonly (string LegacyCode, string CanonicalCode)[] ExplicitLegacyToCanonical =
    [
        ("csacscd", EducationDomainCodes.SoftSkills),
        ("818", EducationDomainCodes.LifeSkills),
        ("itt_77", EducationDomainCodes.TechSkills),
        ("it_1", EducationDomainCodes.Hobbies),
        ("finance_33", EducationDomainCodes.Finance),
        ("skills", EducationDomainCodes.Knowledge),
        ("888", EducationDomainCodes.Sharia),
        ("ititit", EducationDomainCodes.University),
    ];

    public static async Task SeedAsync(ApplicationDBContext context, ILogger? logger = null)
    {
        if (!await SeederHelper.HasAnyDataAsync(context.EducationDomains))
            return;

        // Include inactive custom Qs so keepers like knowledge (Id 4) with deactivated customs win.
        var customQuestionDomainIds = await context.TeacherDomainQuestions
            .AsNoTracking()
            .Where(q => !q.IsSystem)
            .Select(q => q.DomainId)
            .Distinct()
            .ToListAsync();
        var customSet = customQuestionDomainIds.ToHashSet();
        var keepersReceivingCanonical = new HashSet<int>();

        await MergeExplicitLegacyPairsAsync(context, keepersReceivingCanonical, logger);

        var domains = await context.EducationDomains
            .Include(d => d.EducationRule)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .ToListAsync();

        // Bucket A: exact NameAr twins, then NameEn twins (Excel renamed Arabic).
        await MergeNameGroupsAsync(context, domains, customSet, keepersReceivingCanonical, d => NormalizeName(d.NameAr), logger);
        domains = await context.EducationDomains
            .Include(d => d.EducationRule)
            .OrderBy(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .ToListAsync();
        await MergeNameGroupsAsync(context, domains, customSet, keepersReceivingCanonical, d => NormalizeName(d.NameEn), logger);

        await DeactivateTryDomainsAsync(context, logger);

        // Soft-archive legacy skills when Wave-1 split domains all exist and skills has no custom Qs.
        await MaybeArchiveLegacySkillsAsync(context, customSet, logger);

        await ReactivateCustomQuestionsAsync(context, keepersReceivingCanonical, logger);

        await context.SaveChangesAsync();
    }

    private static async Task MergeExplicitLegacyPairsAsync(
        ApplicationDBContext context,
        HashSet<int> keepersReceivingCanonical,
        ILogger? logger)
    {
        foreach (var (legacyCode, canonicalCode) in ExplicitLegacyToCanonical)
        {
            var keeper = await context.EducationDomains
                .Include(d => d.EducationRule)
                .FirstOrDefaultAsync(d => d.IsActive && d.Code == legacyCode);
            var donor = await context.EducationDomains
                .Include(d => d.EducationRule)
                .FirstOrDefaultAsync(d => d.IsActive && d.Code == canonicalCode);

            if (keeper is null || donor is null || keeper.Id == donor.Id)
                continue;

            logger?.LogInformation(
                "Explicit merge: {DonorId}/{DonorCode} into {KeeperId}/{LegacyCode} → {Canonical}",
                donor.Id, donor.Code, keeper.Id, legacyCode, canonicalCode);

            await MergeDonorIntoKeeperAsync(context, keeper, donor, keepersReceivingCanonical, preferredCanonical: canonicalCode);
        }
    }

    private static async Task MergeNameGroupsAsync(
        ApplicationDBContext context,
        List<EducationDomain> domains,
        HashSet<int> customSet,
        HashSet<int> keepersReceivingCanonical,
        Func<EducationDomain, string> keySelector,
        ILogger? logger)
    {
        var groups = domains
            .Where(d => d.IsActive)
            .GroupBy(keySelector)
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
            .ToList();

        foreach (var group in groups)
        {
            var ordered = group
                .OrderByDescending(d => customSet.Contains(d.Id))
                .ThenBy(d => d.CreatedAt)
                .ThenBy(d => d.Id)
                .ToList();

            var keeper = ordered[0];
            foreach (var donor in ordered.Skip(1))
            {
                if (!donor.IsActive)
                    continue;
                logger?.LogInformation(
                    "Merging duplicate domain {DonorId}/{DonorCode} into {KeeperId}/{KeeperCode} (key={Key})",
                    donor.Id, donor.Code, keeper.Id, keeper.Code, keySelector(keeper));

                await MergeDonorIntoKeeperAsync(context, keeper, donor, keepersReceivingCanonical);
            }
        }
    }

    private static string NormalizeName(string? name) =>
        (name ?? string.Empty).Trim().Normalize().ToLowerInvariant();

    /// <summary>
    /// Adopt donor code onto keeper when donor has the Excel code and keeper does not,
    /// including skills → knowledge (both are "canonical" but knowledge supersedes skills).
    /// </summary>
    private static bool ShouldAdoptCodeFromDonor(string keeperCode, string donorCode, string? preferredCanonical)
    {
        if (!string.IsNullOrEmpty(preferredCanonical))
        {
            return string.Equals(donorCode, preferredCanonical, StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(keeperCode, preferredCanonical, StringComparison.OrdinalIgnoreCase);
        }

        if (!CanonicalCodes.Contains(donorCode))
            return false;
        if (string.Equals(keeperCode, donorCode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!CanonicalCodes.Contains(keeperCode))
            return true;

        // Legacy skills row renamed to knowledge twin in prod.
        return string.Equals(keeperCode, EducationDomainCodes.Skills, StringComparison.OrdinalIgnoreCase)
               && EducationDomainCodes.Wave1SplitFromSkills.Contains(donorCode, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task MergeDonorIntoKeeperAsync(
        ApplicationDBContext context,
        EducationDomain keeper,
        EducationDomain donor,
        HashSet<int> keepersReceivingCanonical,
        string? preferredCanonical = null)
    {
        if (keeper.Id == donor.Id)
            return;

        if (ShouldAdoptCodeFromDonor(keeper.Code, donor.Code, preferredCanonical))
        {
            var canonical = preferredCanonical ?? donor.Code;
            donor.Code = $"{donor.Code}-archive-{donor.Id}";
            donor.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(); // free unique index
            keeper.Code = canonical;
            keeper.UpdatedAt = DateTime.UtcNow;
            keepersReceivingCanonical.Add(keeper.Id);
        }

        MergeRules(keeper.EducationRule, donor.EducationRule);

        await RemapDomainIdAsync(context, donor.Id, keeper.Id);
        await MergeTeacherDomainQuestionsAsync(context, donor.Id, keeper.Id);

        donor.IsActive = false;
        if (!donor.NameAr.Contains("أرشيف", StringComparison.Ordinal))
            donor.NameAr = $"{donor.NameAr} (أرشيف)";
        if (!donor.Code.Contains("-archive-", StringComparison.OrdinalIgnoreCase))
            donor.Code = $"{donor.Code}-archive-{donor.Id}";
        donor.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private static void MergeRules(EducationRule? keeper, EducationRule? donor)
    {
        if (keeper is null || donor is null)
            return;

        // Adopt sequence flags from donor (newer Excel rules) without wiping keeper notes.
        keeper.HasCurriculum = keeper.HasCurriculum || donor.HasCurriculum;
        keeper.HasEducationLevel = keeper.HasEducationLevel || donor.HasEducationLevel;
        keeper.HasGrade = keeper.HasGrade || donor.HasGrade;
        keeper.HasAcademicTerm = keeper.HasAcademicTerm || donor.HasAcademicTerm;
        keeper.HasContentUnits = keeper.HasContentUnits || donor.HasContentUnits;
        keeper.HasLessons = keeper.HasLessons || donor.HasLessons;
        keeper.HasUniversity = keeper.HasUniversity || donor.HasUniversity;
        keeper.HasCollege = keeper.HasCollege || donor.HasCollege;
        keeper.HasDepartment = keeper.HasDepartment || donor.HasDepartment;
        keeper.HasAcademicProgram = keeper.HasAcademicProgram || donor.HasAcademicProgram;
        keeper.AcademicTermOptional = keeper.AcademicTermOptional || donor.AcademicTermOptional;
        keeper.RequiresQuranContentType = keeper.RequiresQuranContentType || donor.RequiresQuranContentType;
        keeper.RequiresQuranLevel = keeper.RequiresQuranLevel || donor.RequiresQuranLevel;
        keeper.RequiresUnitTypeSelection = keeper.RequiresUnitTypeSelection || donor.RequiresUnitTypeSelection;
        keeper.HasParentSubject = keeper.HasParentSubject || donor.HasParentSubject;
        keeper.EducationLevelAfterSubject =
            keeper.EducationLevelAfterSubject || donor.EducationLevelAfterSubject;
        keeper.HasWritableFilters = keeper.HasWritableFilters || donor.HasWritableFilters;
        keeper.RulesConfigured = true;

        // Prefer donor numeric defaults when donor looks like Excel-configured.
        if (donor.RulesConfigured || donor.HasWritableFilters || donor.HasParentSubject)
        {
            keeper.MinSessions = donor.MinSessions;
            keeper.MaxSessions = donor.MaxSessions;
            keeper.DefaultSessionDurationMinutes = donor.DefaultSessionDurationMinutes;
            keeper.MinGroupSize = donor.MinGroupSize ?? keeper.MinGroupSize;
            keeper.MaxGroupSize = donor.MaxGroupSize ?? keeper.MaxGroupSize;
            keeper.AllowExtension = donor.AllowExtension;
            keeper.AllowFlexibleCourses = donor.AllowFlexibleCourses;
        }

        // Quran lessons flag is intentionally false in Excel seed — take donor when quran-ish.
        if (donor.RequiresQuranContentType || donor.RequiresUnitTypeSelection)
        {
            keeper.HasLessons = donor.HasLessons;
            keeper.RequiresQuranLevel = donor.RequiresQuranLevel;
        }

        keeper.UpdatedAt = DateTime.UtcNow;
    }

    private static string DedupName(string name, int id, int maxLength)
    {
        var suffix = $"-dup-{id}";
        if (name.Length + suffix.Length <= maxLength)
            return name + suffix;
        var keep = Math.Max(0, maxLength - suffix.Length);
        return name[..keep] + suffix;
    }

    private static async Task RemapDomainIdAsync(ApplicationDBContext context, int fromId, int toId)
    {
        // Subjects: rename colliding codes then move.
        var keeperSubjectCodes = await context.Subjects
            .Where(s => s.DomainId == toId)
            .Select(s => s.Code)
            .ToListAsync();
        var keeperSubjectCodeSet = keeperSubjectCodes
            .Where(c => c != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        var donorSubjects = await context.Subjects.Where(s => s.DomainId == fromId).ToListAsync();
        foreach (var subject in donorSubjects)
        {
            if (subject.Code != null && keeperSubjectCodeSet.Contains(subject.Code))
                subject.Code = DedupName(subject.Code, subject.Id, 80);
            subject.DomainId = toId;
        }

        // Writable slots — unique (DomainId, Code)
        var keeperSlotCodes = await context.WritableFilterSlots
            .Where(s => s.DomainId == toId)
            .Select(s => s.Code)
            .ToListAsync();
        var keeperSlotSet = keeperSlotCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var donorSlots = await context.WritableFilterSlots.Where(s => s.DomainId == fromId).ToListAsync();
        foreach (var slot in donorSlots)
        {
            if (keeperSlotSet.Contains(slot.Code))
                slot.Code = DedupName(slot.Code, slot.Id, 80);
            slot.DomainId = toId;
        }

        // Curriculums — unique (DomainId, NameEn)
        var keeperCurriculumNames = await context.Curriculums
            .Where(c => c.DomainId == toId)
            .Select(c => c.NameEn)
            .ToListAsync();
        var keeperCurriculumNameSet = keeperCurriculumNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var donorCurriculums = await context.Curriculums.Where(c => c.DomainId == fromId).ToListAsync();
        foreach (var row in donorCurriculums)
        {
            if (keeperCurriculumNameSet.Contains(row.NameEn))
                row.NameEn = DedupName(row.NameEn, row.Id, 100);
            row.DomainId = toId;
        }

        // EducationLevels — unique (DomainId, CurriculumId, NameEn)
        var keeperLevelKeys = (await context.EducationLevels
                .Where(l => l.DomainId == toId)
                .Select(l => new { l.CurriculumId, l.NameEn })
                .ToListAsync())
            .Select(l => $"{l.CurriculumId?.ToString() ?? "null"}|{l.NameEn}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var donorLevels = await context.EducationLevels.Where(l => l.DomainId == fromId).ToListAsync();
        foreach (var row in donorLevels)
        {
            var key = $"{row.CurriculumId?.ToString() ?? "null"}|{row.NameEn}";
            if (keeperLevelKeys.Contains(key))
                row.NameEn = DedupName(row.NameEn, row.Id, 100);
            row.DomainId = toId;
        }

        var keeperModeIds = await context.DomainTeachingModes
            .Where(x => x.DomainId == toId)
            .Select(x => x.TeachingModeId)
            .ToListAsync();
        var keeperModeSet = keeperModeIds.ToHashSet();
        var donorModes = await context.DomainTeachingModes.Where(x => x.DomainId == fromId).ToListAsync();
        foreach (var mode in donorModes)
        {
            if (keeperModeSet.Contains(mode.TeachingModeId))
                context.DomainTeachingModes.Remove(mode);
            else
                mode.DomainId = toId;
        }

        foreach (var row in await context.DomainSessionPrices.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;
        foreach (var row in await context.PricingSnapshots.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;

        // TeacherDomainApprovals — unique (TeacherId, DomainId): keep keeper, drop donor twin
        var keeperApprovalTeacherIds = (await context.TeacherDomainApprovals
                .Where(x => x.DomainId == toId)
                .Select(x => x.TeacherId)
                .ToListAsync())
            .ToHashSet();
        foreach (var row in await context.TeacherDomainApprovals.Where(x => x.DomainId == fromId).ToListAsync())
        {
            if (keeperApprovalTeacherIds.Contains(row.TeacherId))
                context.TeacherDomainApprovals.Remove(row);
            else
                row.DomainId = toId;
        }

        // TeacherDomainPricings — unique (TeacherId, DomainId)
        var keeperPricingTeacherIds = (await context.TeacherDomainPricings
                .Where(x => x.DomainId == toId)
                .Select(x => x.TeacherId)
                .ToListAsync())
            .ToHashSet();
        foreach (var row in await context.TeacherDomainPricings.Where(x => x.DomainId == fromId).ToListAsync())
        {
            if (keeperPricingTeacherIds.Contains(row.TeacherId))
                context.TeacherDomainPricings.Remove(row);
            else
                row.DomainId = toId;
        }

        foreach (var row in await context.TeacherLevelUpgradeSuggestions.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;
        foreach (var row in await context.Students.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;
        foreach (var row in await context.StudentFreeTrialConsumptions.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;
        foreach (var row in await context.OpenSessionRequests.Where(x => x.DomainId == fromId).ToListAsync())
            row.DomainId = toId;

        await context.SaveChangesAsync();
    }

    private static async Task MergeTeacherDomainQuestionsAsync(
        ApplicationDBContext context,
        int fromDomainId,
        int toDomainId)
    {
        var donorQs = await context.TeacherDomainQuestions
            .Where(q => q.DomainId == fromDomainId)
            .ToListAsync();
        var keeperQs = await context.TeacherDomainQuestions
            .Where(q => q.DomainId == toDomainId)
            .ToListAsync();
        var keeperByCode = keeperQs.ToDictionary(q => q.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var q in donorQs)
        {
            if (!keeperByCode.TryGetValue(q.Code, out var existing))
            {
                q.DomainId = toDomainId;
                q.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            // Custom donor wins over system keeper short question.
            if (existing.IsSystem && !q.IsSystem)
            {
                existing.IsActive = false;
                existing.UpdatedAt = DateTime.UtcNow;
                q.DomainId = toDomainId;
                q.UpdatedAt = DateTime.UtcNow;
                // Avoid unique (DomainId, Code): change archived system code.
                existing.Code = $"{existing.Code}-seed-archive-{existing.Id}";
            }
            else
            {
                // Keep keeper; deactivate donor copy.
                q.IsActive = false;
                q.UpdatedAt = DateTime.UtcNow;
                q.Code = $"{q.Code}-dup-archive-{q.Id}";
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task DeactivateTryDomainsAsync(ApplicationDBContext context, ILogger? logger)
    {
        var tryDomains = await context.EducationDomains
            .Where(d => d.IsActive && d.Code.StartsWith("try_"))
            .ToListAsync();
        foreach (var d in tryDomains)
        {
            d.IsActive = false;
            d.UpdatedAt = DateTime.UtcNow;
            logger?.LogInformation("Deactivated try domain {Id}/{Code}", d.Id, d.Code);
        }
    }

    private static async Task ReactivateCustomQuestionsAsync(
        ApplicationDBContext context,
        HashSet<int> keepersReceivingCanonical,
        ILogger? logger)
    {
        if (keepersReceivingCanonical.Count == 0)
            return;

        var inactiveCustoms = await context.TeacherDomainQuestions
            .Where(q =>
                keepersReceivingCanonical.Contains(q.DomainId) &&
                !q.IsSystem &&
                !q.IsActive)
            .ToListAsync();

        foreach (var q in inactiveCustoms)
        {
            q.IsActive = true;
            q.UpdatedAt = DateTime.UtcNow;
        }

        if (inactiveCustoms.Count > 0)
        {
            logger?.LogInformation(
                "Reactivated {Count} inactive custom domain questions on keepers that received canonical codes",
                inactiveCustoms.Count);
        }
    }

    private static async Task MaybeArchiveLegacySkillsAsync(
        ApplicationDBContext context,
        HashSet<int> customQuestionDomainIds,
        ILogger? logger)
    {
        var skills = await context.EducationDomains
            .FirstOrDefaultAsync(d => d.Code == EducationDomainCodes.Skills);
        if (skills is null || !skills.IsActive)
            return;

        if (customQuestionDomainIds.Contains(skills.Id))
            return; // keep approved skills with custom questions

        var wave1Present = await context.EducationDomains
            .CountAsync(d =>
                d.IsActive && EducationDomainCodes.Wave1SplitFromSkills.Contains(d.Code));
        if (wave1Present < EducationDomainCodes.Wave1SplitFromSkills.Length)
            return;

        skills.IsActive = false;
        if (!skills.NameAr.Contains("أرشيف", StringComparison.Ordinal))
            skills.NameAr = $"{skills.NameAr} (أرشيف)";
        skills.UpdatedAt = DateTime.UtcNow;
        logger?.LogInformation("Archived legacy skills domain {Id} — Wave-1 domains are active", skills.Id);
    }
}
