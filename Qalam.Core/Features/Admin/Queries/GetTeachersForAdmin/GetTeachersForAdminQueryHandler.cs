using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeachersForAdmin;

public class GetTeachersForAdminQueryHandler : ResponseHandler,
    IRequestHandler<GetTeachersForAdminQuery, Response<List<AdminTeacherListItemDto>>>
{
    private const int MaxPageSize = 50;

    private readonly ITeacherRepository _teacherRepository;

    public GetTeachersForAdminQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<AdminTeacherListItemDto>>> Handle(
        GetTeachersForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize
        };

        TeacherStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TeacherStatus>(request.Status.Trim(), ignoreCase: true, out var parsed))
            {
                return BadRequest<List<AdminTeacherListItemDto>>(
                    "Invalid status. Valid values: AwaitingDocuments, PendingVerification, DocumentsRejected, Active, Blocked");
            }

            statusFilter = parsed;
        }

        var filters = new AdminTeacherListFilters(
            Status: statusFilter,
            Location: request.Location,
            SubjectId: request.SubjectId,
            Search: request.Search,
            SortBy: request.SortBy,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _teacherRepository.SearchForAdminAsync(filters, cancellationToken);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
