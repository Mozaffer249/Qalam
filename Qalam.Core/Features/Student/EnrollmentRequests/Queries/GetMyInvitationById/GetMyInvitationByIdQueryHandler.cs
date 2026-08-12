using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitationById;

public class GetMyInvitationByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyInvitationByIdQuery, Response<StudentInvitationDetailDto>>
{
    private readonly IStudentInvitationInboxService _inboxService;

    public GetMyInvitationByIdQueryHandler(
        IStudentInvitationInboxService inboxService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _inboxService = inboxService;
    }

    public async Task<Response<StudentInvitationDetailDto>> Handle(
        GetMyInvitationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!StudentInvitationDetailDto.TryParseInvitationKey(request.InvitationKey, out _, out _))
        {
            return BadRequest<StudentInvitationDetailDto>(
                "InvitationKey must be EnrollmentRequest-{id} or OpenSessionRequest-{id}.");
        }

        var detail = await _inboxService.GetInvitationDetailAsync(
            request.UserId,
            request.InvitationKey,
            cancellationToken);

        if (detail == null)
            return NotFound<StudentInvitationDetailDto>("Invitation not found.");

        return Success(entity: detail);
    }
}
