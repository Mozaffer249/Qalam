using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetEnrollmentConversationMessages;

public class GetEnrollmentConversationMessagesQueryHandler : ResponseHandler,
    IRequestHandler<GetEnrollmentConversationMessagesQuery, Response<EnrollmentConversationMessagesPageDto>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetEnrollmentConversationMessagesQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<EnrollmentConversationMessagesPageDto>> Handle(
        GetEnrollmentConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var (page, forbidden) = await _enrollmentService.GetConversationMessagesAsync(
            request.UserId,
            request.ConversationId,
            request.Cursor,
            request.Direction,
            request.Take,
            cancellationToken);

        if (forbidden || page == null)
            return Forbidden<EnrollmentConversationMessagesPageDto>("NOT_A_PARTICIPANT");

        return Success(entity: page);
    }
}
