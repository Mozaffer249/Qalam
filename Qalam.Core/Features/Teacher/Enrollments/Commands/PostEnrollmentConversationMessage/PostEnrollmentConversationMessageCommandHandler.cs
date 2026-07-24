using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Commands.PostEnrollmentConversationMessage;

public class PostEnrollmentConversationMessageCommandHandler : ResponseHandler,
    IRequestHandler<PostEnrollmentConversationMessageCommand, Response<EnrollmentConversationMessageDto>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public PostEnrollmentConversationMessageCommandHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<EnrollmentConversationMessageDto>> Handle(
        PostEnrollmentConversationMessageCommand request,
        CancellationToken cancellationToken)
    {
        var (dto, error, forbidden) = await _enrollmentService.PostConversationMessageAsync(
            request.UserId,
            request.ConversationId,
            request.Data?.Content,
            cancellationToken);

        if (forbidden)
            return Forbidden<EnrollmentConversationMessageDto>(error ?? "NOT_A_PARTICIPANT");

        if (dto == null)
            return BadRequest<EnrollmentConversationMessageDto>(error ?? "Unable to post message.");

        return Created(entity: dto);
    }
}
