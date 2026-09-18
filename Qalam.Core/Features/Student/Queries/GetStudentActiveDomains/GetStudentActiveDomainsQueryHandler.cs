using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetStudentActiveDomains;

public class GetStudentActiveDomainsQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentActiveDomainsQuery, Response<List<StudentEducationDomainDto>>>
{
    /// <summary>Excel Sheet1 domain display order (codes) — matches student Flutter wizard.</summary>
    private static readonly string[] ExcelDomainOrder =
    [
        "school",
        "university",
        "quran",
        "sharia",
        "language",
        "tech-skills",
        "soft-skills",
        "life-skills",
        "hobbies",
        "finance",
        "knowledge",
    ];

    private readonly IEducationDomainService _domainService;

    public GetStudentActiveDomainsQueryHandler(
        IEducationDomainService domainService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<List<StudentEducationDomainDto>>> Handle(
        GetStudentActiveDomainsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _domainService.GetActiveDomainsQueryable()
            .Select(d => new StudentEducationDomainDto
            {
                Id = d.Id,
                NameAr = d.NameAr,
                NameEn = d.NameEn,
                Code = d.Code,
                DescriptionAr = d.DescriptionAr,
                DescriptionEn = d.DescriptionEn,
                IsActive = d.IsActive,
            })
            .ToListAsync(cancellationToken);

        items.Sort(CompareExcelOrder);

        return Success(entity: items);
    }

    private static int CompareExcelOrder(StudentEducationDomainDto a, StudentEducationDomainDto b)
    {
        var ra = Rank(a.Code, a.Id);
        var rb = Rank(b.Code, b.Id);
        if (ra != rb) return ra.CompareTo(rb);
        return string.Compare(a.NameAr, b.NameAr, StringComparison.OrdinalIgnoreCase);
    }

    private static int Rank(string? code, int id)
    {
        var normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        var idx = Array.IndexOf(ExcelDomainOrder, normalized);
        return idx >= 0 ? idx : ExcelDomainOrder.Length + id;
    }
}
