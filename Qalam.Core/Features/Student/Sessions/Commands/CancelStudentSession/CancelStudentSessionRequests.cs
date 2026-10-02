using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Features.Student.Enrollments.Commands.CancelEnrollment;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Policy;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Core.Features.Student.Sessions.Commands.CancelStudentSession;

public class GetStudentSessionCancelPreviewQuery : IRequest<Response<PolicyPreviewDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int ScheduleId { get; set; }
    public PolicyStudentChoice Choice { get; set; } = PolicyStudentChoice.Refund;
}

public class CancelStudentSessionCommand : IRequest<Response<PolicyOutcomeDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int ScheduleId { get; set; }
    public PolicyStudentChoice Choice { get; set; } = PolicyStudentChoice.Refund;
    public DateOnly? NewDate { get; set; }
    public int? NewTeacherAvailabilityId { get; set; }
    public string? Reason { get; set; }
}

public class CancelStudentSessionHandlers : ResponseHandler,
    IRequestHandler<GetStudentSessionCancelPreviewQuery, Response<PolicyPreviewDto>>,
    IRequestHandler<CancelStudentSessionCommand, Response<PolicyOutcomeDto>>
{
    private readonly ISessionPolicyService _sessionPolicy;

    public CancelStudentSessionHandlers(
        ISessionPolicyService sessionPolicy,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _sessionPolicy = sessionPolicy;
    }

    public async Task<Response<PolicyPreviewDto>> Handle(GetStudentSessionCancelPreviewQuery request, CancellationToken cancellationToken)
    {
        var preview = await _sessionPolicy.PreviewStudentCancelAsync(
            request.ScheduleId, request.UserId, request.Choice, cancellationToken);
        return preview == null ? NotFound<PolicyPreviewDto>("Session not found.") : Success(entity: preview);
    }

    public async Task<Response<PolicyOutcomeDto>> Handle(CancelStudentSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _sessionPolicy.StudentCancelAsync(
                request.ScheduleId,
                request.UserId,
                new StudentSessionCancelRequest
                {
                    Choice = request.Choice,
                    NewDate = request.NewDate,
                    NewTeacherAvailabilityId = request.NewTeacherAvailabilityId,
                    Reason = request.Reason
                },
                cancellationToken);
            return Success(entity: outcome);
        }
        catch (PolicyDeniedException ex)
        {
            return BadRequest<PolicyOutcomeDto>(CancelEnrollmentCommandHandler.Localized(ex));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PolicyOutcomeDto>(ex.Message);
        }
    }
}
