using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequests;

public class GetAvailableRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetAvailableRequestsQuery, Response<PaginatedResult<TeacherAvailableRequestListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;

    public GetAvailableRequestsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepo,
        IOpenSessionRequestTargetRepository targetRepo) : base(localizer)
    {
        _teacherRepo = teacherRepo;
        _targetRepo = targetRepo;
    }

    public async Task<Response<PaginatedResult<TeacherAvailableRequestListItemDto>>> Handle(
        GetAvailableRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
        if (teacher == null || teacher.Status != TeacherStatus.Active)
            return Unauthorized<PaginatedResult<TeacherAvailableRequestListItemDto>>("Teacher account not active.");

        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var pageNumber = Math.Max(1, request.PageNumber);

        var filters = new TeacherInboxFilters(
            request.Status,
            request.SubjectId,
            request.DateFrom,
            request.DateTo,
            pageNumber,
            pageSize,
            request.SortBy);

        var page = await _targetRepo.GetTeacherInboxAsync(teacher.Id, filters, cancellationToken);
        return Success(entity: page);
    }
}
