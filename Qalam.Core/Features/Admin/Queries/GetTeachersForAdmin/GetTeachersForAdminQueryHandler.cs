using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeachersForAdmin;

public class GetTeachersForAdminQueryHandler : ResponseHandler,
    IRequestHandler<GetTeachersForAdminQuery, Response<List<AdminTeacherListItemDto>>>
{
    private const int MaxPageSize = 100;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherRegistrationStatusService _registrationStatusService;

    public GetTeachersForAdminQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherRegistrationStatusService registrationStatusService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _registrationStatusService = registrationStatusService;
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

        TeacherRequirementFilterStatus? requirementStatus = null;
        if (!string.IsNullOrWhiteSpace(request.RequirementStatus))
        {
            if (!Enum.TryParse<TeacherRequirementFilterStatus>(
                    request.RequirementStatus.Trim(), ignoreCase: true, out var parsedReqStatus))
            {
                return BadRequest<List<AdminTeacherListItemDto>>(
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
            PageNumber: pageNumber,
            PageSize: pageSize,
            DomainId: request.DomainId,
            CreatedFrom: request.CreatedFrom?.Date,
            CreatedTo: createdTo,
            RequirementCode: request.RequirementCode,
            RequirementStatus: requirementStatus,
            MissingTeacherLevel: request.MissingTeacherLevel);

        var result = await _teacherRepository.SearchForAdminAsync(filters, cancellationToken);

        var teacherIds = result.Items.Select(i => i.TeacherId).ToList();
        var checklists = await _registrationStatusService.GetChecklistsForTeachersAsync(teacherIds, cancellationToken);
        foreach (var item in result.Items)
            item.RegistrationRequirements = checklists.GetValueOrDefault(item.TeacherId) ?? [];

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
