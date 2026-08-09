using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.ExportTeachersForAdmin;

public class ExportTeachersForAdminQuery : IRequest<Response<AdminTeacherCsvExportDto>>
{
    public string? Status { get; set; }
    public TeacherLocation? Location { get; set; }
    public int? SubjectId { get; set; }
    public int? DomainId { get; set; }
    public string? Search { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public AdminTeacherListSort SortBy { get; set; } = AdminTeacherListSort.Newest;
    public string? RequirementCode { get; set; }
    public string? RequirementStatus { get; set; }
}

public class ExportTeachersForAdminQueryHandler : ResponseHandler,
    IRequestHandler<ExportTeachersForAdminQuery, Response<AdminTeacherCsvExportDto>>
{
    public const int MaxExportRows = 10_000;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherRegistrationStatusService _registrationStatusService;

    public ExportTeachersForAdminQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherRegistrationStatusService registrationStatusService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _registrationStatusService = registrationStatusService;
    }

    public async Task<Response<AdminTeacherCsvExportDto>> Handle(
        ExportTeachersForAdminQuery request,
        CancellationToken cancellationToken)
    {
        TeacherStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TeacherStatus>(request.Status.Trim(), ignoreCase: true, out var parsed))
            {
                return BadRequest<AdminTeacherCsvExportDto>(
                    "Invalid status. Valid values: AwaitingDocuments, PendingVerification, DocumentsRejected, Active, Blocked");
            }

            statusFilter = parsed;
        }

        TeacherRequirementFilterStatus? requirementStatus = null;
        if (!string.IsNullOrWhiteSpace(request.RequirementStatus))
        {
            if (!Enum.TryParse<TeacherRequirementFilterStatus>(
                    request.RequirementStatus.Trim(), ignoreCase: true, out var parsedReqStatus))
            {
                return BadRequest<AdminTeacherCsvExportDto>(
                    "Invalid requirementStatus. Valid values: Submitted, NotSubmitted, Pending, Approved, Rejected");
            }

            requirementStatus = parsedReqStatus;
        }

        var createdTo = request.CreatedTo;
        if (createdTo.HasValue && createdTo.Value.TimeOfDay == TimeSpan.Zero)
            createdTo = createdTo.Value.Date.AddDays(1).AddTicks(-1);

        var filters = new AdminTeacherListFilters(
            Status: statusFilter,
            Location: request.Location,
            SubjectId: request.SubjectId,
            Search: request.Search,
            SortBy: request.SortBy,
            PageNumber: 1,
            PageSize: MaxExportRows,
            DomainId: request.DomainId,
            CreatedFrom: request.CreatedFrom?.Date,
            CreatedTo: createdTo,
            RequirementCode: request.RequirementCode,
            RequirementStatus: requirementStatus);

        var items = await _teacherRepository.ExportForAdminAsync(filters, MaxExportRows, cancellationToken);
        if (items == null)
        {
            return BadRequest<AdminTeacherCsvExportDto>(
                $"Export exceeds the maximum of {MaxExportRows} rows. Narrow your filters and try again.");
        }

        var teacherIds = items.Select(i => i.TeacherId).ToList();
        var checklists = await _registrationStatusService.GetChecklistsForTeachersAsync(teacherIds, cancellationToken);
        foreach (var item in items)
            item.RegistrationRequirements = checklists.GetValueOrDefault(item.TeacherId) ?? [];

        return Success(entity: new AdminTeacherCsvExportDto
        {
            Content = AdminTeacherCsvHelper.BuildCsvBytes(items),
            FileName = $"teachers-export-{DateTime.UtcNow:yyyyMMdd}.csv"
        });
    }
}
