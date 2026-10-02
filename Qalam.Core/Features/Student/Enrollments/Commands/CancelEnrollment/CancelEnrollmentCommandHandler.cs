using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Policy;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models.Policy;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<CancelEnrollmentCommand, Response<PolicyOutcomeDto>>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentCancellationService _cancellationService;

    public CancelEnrollmentCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentCancellationService cancellationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentRepository = enrollmentRepository;
        _cancellationService = cancellationService;
    }

    public async Task<Response<PolicyOutcomeDto>> Handle(
        CancelEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.EnrollmentRequest)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
            return NotFound<PolicyOutcomeDto>("Enrollment not found.");

        var ownerUserId = enrollment.OwnerUserId
                          ?? enrollment.EnrollmentRequest?.RequestedByUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<PolicyOutcomeDto>("Only the enrollment owner can cancel this enrollment.");

        try
        {
            var outcome = await _cancellationService.CancelAsync(
                request.EnrollmentId,
                request.UserId,
                reason: "Student cancelled enrollment",
                cancellationToken);

            return Success(entity: outcome ?? new PolicyOutcomeDto
            {
                Allowed = true,
                Kind = PolicyCaseKind.BeforeFirstSessionCancel.ToString(),
                CancelsEnrollment = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (PolicyDeniedException ex)
        {
            return BadRequest<PolicyOutcomeDto>(Localized(ex));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<PolicyOutcomeDto>(ex.Message);
        }
    }

    internal static string Localized(PolicyDeniedException ex)
        => ex.Explanation != null && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
            ? ex.Explanation.Ar
            : ex.Message;
}
