using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Queries.GetOrCreateEnrollmentConversation;

public class GetOrCreateEnrollmentConversationQueryHandler : ResponseHandler,
    IRequestHandler<GetOrCreateEnrollmentConversationQuery, Response<EnrollmentConversationDto>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public GetOrCreateEnrollmentConversationQueryHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<EnrollmentConversationDto>> Handle(
        GetOrCreateEnrollmentConversationQuery request,
        CancellationToken cancellationToken)
    {
        var (dto, error, forbidden) = await _enrollmentService.GetOrCreateConversationAsync(
            request.UserId, request.EnrollmentId, cancellationToken);

        if (forbidden)
            return Forbidden<EnrollmentConversationDto>(error ?? "NOT_A_PARTICIPANT");

        if (dto == null)
        {
            if (error != null && error.Contains("no student", StringComparison.OrdinalIgnoreCase))
                return BadRequest<EnrollmentConversationDto>(error);
            return NotFound<EnrollmentConversationDto>(error ?? "Enrollment not found.");
        }

        return Success(entity: dto);
    }
}
