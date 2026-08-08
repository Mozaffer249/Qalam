using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

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
}

public class ExportTeachersForAdminQueryHandler : ResponseHandler,
    IRequestHandler<ExportTeachersForAdminQuery, Response<AdminTeacherCsvExportDto>>
{
    public const int MaxExportRows = 10_000;

    private readonly ITeacherRepository _teacherRepository;

    public ExportTeachersForAdminQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
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
            CreatedTo: createdTo);

        var items = await _teacherRepository.ExportForAdminAsync(filters, MaxExportRows, cancellationToken);
        if (items == null)
        {
            return BadRequest<AdminTeacherCsvExportDto>(
                $"Export exceeds the maximum of {MaxExportRows} rows. Narrow your filters and try again.");
        }

        return Success(entity: new AdminTeacherCsvExportDto
        {
            Content = AdminTeacherCsvHelper.BuildCsvBytes(items),
            FileName = $"teachers-export-{DateTime.UtcNow:yyyyMMdd}.csv"
        });
    }
}
