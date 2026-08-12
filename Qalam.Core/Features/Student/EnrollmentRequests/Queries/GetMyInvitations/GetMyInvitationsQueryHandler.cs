using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitations;

public class GetMyInvitationsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyInvitationsQuery, Response<List<StudentInvitationListItemDto>>>
{
    private readonly IStudentInvitationInboxService _inboxService;

    public GetMyInvitationsQueryHandler(
        IStudentInvitationInboxService inboxService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _inboxService = inboxService;
    }

    public async Task<Response<List<StudentInvitationListItemDto>>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _inboxService.GetMyInvitationsAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.Scope,
            cancellationToken);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, result.TotalCount));
    }
}
