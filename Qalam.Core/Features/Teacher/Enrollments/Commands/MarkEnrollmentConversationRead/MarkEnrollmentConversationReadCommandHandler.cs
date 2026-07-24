using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Commands.MarkEnrollmentConversationRead;

public class MarkEnrollmentConversationReadCommandHandler : ResponseHandler,
    IRequestHandler<MarkEnrollmentConversationReadCommand, Response<string>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public MarkEnrollmentConversationReadCommandHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<string>> Handle(
        MarkEnrollmentConversationReadCommand request,
        CancellationToken cancellationToken)
    {
        var (ok, forbidden) = await _enrollmentService.MarkConversationReadAsync(
            request.UserId, request.ConversationId, cancellationToken);

        if (forbidden || !ok)
            return Forbidden<string>("NOT_A_PARTICIPANT");

        return Success(entity: "تم تحديث حالة القراءة");
    }
}
