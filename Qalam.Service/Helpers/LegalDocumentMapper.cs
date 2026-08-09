using Qalam.Data.Commons;
using Qalam.Data.DTOs.Legal;
using Qalam.Data.Entity.Legal;

namespace Qalam.Service.Helpers;

public static class LegalDocumentMapper
{
    public static LegalDocumentListItemDto ToListItem(LegalDocument doc)
    {
        var published = doc.CurrentPublishedVersion;
        return new LegalDocumentListItemDto
        {
            Id = doc.Id,
            Code = doc.Code,
            TitleAr = doc.TitleAr,
            TitleEn = doc.TitleEn,
            DisplayOrder = doc.DisplayOrder,
            IsActive = doc.IsActive,
            RequiresConsent = doc.RequiresConsent,
            HasArabic = !string.IsNullOrWhiteSpace(doc.TitleAr),
            HasEnglish = !string.IsNullOrWhiteSpace(doc.TitleEn),
            CurrentVersionLabel = published?.VersionLabel,
            CurrentVersionStatus = published?.Status,
            PublishedAt = published?.PublishedAt,
            LastUpdatedAt = doc.UpdatedAt ?? doc.CreatedAt
        };
    }

    public static LegalDocumentVersionSummaryDto ToVersionSummary(LegalDocumentVersion v) => new()
    {
        Id = v.Id,
        LegalDocumentId = v.LegalDocumentId,
        MajorVersion = v.MajorVersion,
        MinorVersion = v.MinorVersion,
        VersionLabel = v.VersionLabel,
        Status = v.Status,
        ChangeNotes = v.ChangeNotes,
        EffectiveDate = v.EffectiveDate,
        PublishedAt = v.PublishedAt,
        PublishedByUserId = v.PublishedByUserId,
        ArchivedAt = v.ArchivedAt,
        CreatedAt = v.CreatedAt,
        CreatedBy = v.CreatedBy
    };

    public static LegalDocumentVersionDetailDto ToVersionDetail(LegalDocumentVersion v)
    {
        var dto = new LegalDocumentVersionDetailDto
        {
            Id = v.Id,
            LegalDocumentId = v.LegalDocumentId,
            MajorVersion = v.MajorVersion,
            MinorVersion = v.MinorVersion,
            VersionLabel = v.VersionLabel,
            Status = v.Status,
            ChangeNotes = v.ChangeNotes,
            EffectiveDate = v.EffectiveDate,
            PublishedAt = v.PublishedAt,
            PublishedByUserId = v.PublishedByUserId,
            ArchivedAt = v.ArchivedAt,
            CreatedAt = v.CreatedAt,
            CreatedBy = v.CreatedBy,
            DocumentCode = v.LegalDocument?.Code ?? string.Empty,
            DocumentTitleAr = v.LegalDocument?.TitleAr ?? string.Empty,
            DocumentTitleEn = v.LegalDocument?.TitleEn ?? string.Empty,
            Sections = BuildSectionTree(v.Sections, enabledOnly: false)
        };
        return dto;
    }

    public static List<LegalDocumentSectionDto> BuildSectionTree(
        IEnumerable<LegalDocumentSection> sections,
        bool enabledOnly)
    {
        var list = sections
            .Where(s => !enabledOnly || s.IsEnabled)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToList();

        var lookup = list.ToDictionary(
            s => s.Id,
            s => new LegalDocumentSectionDto
            {
                Id = s.Id,
                LegalDocumentVersionId = s.LegalDocumentVersionId,
                ParentSectionId = s.ParentSectionId,
                AnchorKey = s.AnchorKey,
                TitleAr = s.TitleAr,
                TitleEn = s.TitleEn,
                ContentAr = s.ContentAr,
                ContentEn = s.ContentEn,
                DisplayOrder = s.DisplayOrder,
                IsEnabled = s.IsEnabled
            });

        var roots = new List<LegalDocumentSectionDto>();
        foreach (var s in list)
        {
            var dto = lookup[s.Id];
            if (s.ParentSectionId.HasValue && lookup.TryGetValue(s.ParentSectionId.Value, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }

        return roots;
    }

    public static PublicLegalDocumentSummaryDto ToPublicSummary(LegalDocument doc)
    {
        var v = doc.CurrentPublishedVersion!;
        return new PublicLegalDocumentSummaryDto
        {
            Code = doc.Code,
            Title = LocalizableEntity.GetLocalizedValue(doc.TitleAr, doc.TitleEn) ?? doc.TitleEn,
            VersionLabel = v.VersionLabel,
            EffectiveDate = v.EffectiveDate,
            PublishedAt = v.PublishedAt
        };
    }

    public static PublicLegalDocumentDto ToPublicDocument(LegalDocumentVersion v)
    {
        var doc = v.LegalDocument;
        return new PublicLegalDocumentDto
        {
            Code = doc.Code,
            Title = LocalizableEntity.GetLocalizedValue(doc.TitleAr, doc.TitleEn) ?? doc.TitleEn,
            VersionLabel = v.VersionLabel,
            EffectiveDate = v.EffectiveDate,
            PublishedAt = v.PublishedAt,
            Sections = BuildPublicSectionTree(v.Sections)
        };
    }

    public static List<PublicLegalSectionDto> BuildPublicSectionTree(IEnumerable<LegalDocumentSection> sections)
    {
        var list = sections
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToList();

        var lookup = list.ToDictionary(
            s => s.Id,
            s => new PublicLegalSectionDto
            {
                AnchorKey = s.AnchorKey,
                Title = LocalizableEntity.GetLocalizedValue(s.TitleAr, s.TitleEn) ?? s.TitleEn,
                Content = LocalizableEntity.GetLocalizedValue(s.ContentAr, s.ContentEn),
                DisplayOrder = s.DisplayOrder
            });

        var roots = new List<PublicLegalSectionDto>();
        foreach (var s in list)
        {
            var dto = lookup[s.Id];
            if (s.ParentSectionId.HasValue && lookup.TryGetValue(s.ParentSectionId.Value, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }

        return roots;
    }

    public static PendingConsentDocumentDto ToPendingConsent(LegalDocument doc)
    {
        var v = doc.CurrentPublishedVersion!;
        return new PendingConsentDocumentDto
        {
            Code = doc.Code,
            Title = LocalizableEntity.GetLocalizedValue(doc.TitleAr, doc.TitleEn) ?? doc.TitleEn,
            VersionId = v.Id,
            VersionLabel = v.VersionLabel,
            EffectiveDate = v.EffectiveDate
        };
    }

    /// <summary>Deep-copies sections from source into newVersion (in-memory, before save).</summary>
    public static void CloneSections(LegalDocumentVersion source, LegalDocumentVersion target)
    {
        var ordered = source.Sections
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToList();

        var idMap = new Dictionary<int, LegalDocumentSection>();

        // First pass: create all without parents
        foreach (var src in ordered)
        {
            var copy = new LegalDocumentSection
            {
                AnchorKey = src.AnchorKey,
                TitleAr = src.TitleAr,
                TitleEn = src.TitleEn,
                ContentAr = src.ContentAr,
                ContentEn = src.ContentEn,
                DisplayOrder = src.DisplayOrder,
                IsEnabled = src.IsEnabled,
                CreatedAt = DateTime.UtcNow
            };
            idMap[src.Id] = copy;
            target.Sections.Add(copy);
        }

        // Second pass: wire parents (EF will fix FKs after insert via navigation)
        foreach (var src in ordered)
        {
            if (src.ParentSectionId.HasValue && idMap.TryGetValue(src.ParentSectionId.Value, out var parentCopy))
                idMap[src.Id].ParentSection = parentCopy;
        }
    }
}
