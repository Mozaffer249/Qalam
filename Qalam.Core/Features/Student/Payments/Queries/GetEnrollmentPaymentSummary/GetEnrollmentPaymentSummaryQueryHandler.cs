using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Queries.GetEnrollmentPaymentSummary;

public class GetEnrollmentPaymentSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetEnrollmentPaymentSummaryQuery, Response<EnrollmentPaymentSummaryDto>>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _settings;

    public GetEnrollmentPaymentSummaryQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentRepository = enrollmentRepository;
        _settings = settings.Value;
    }

    public async Task<Response<EnrollmentPaymentSummaryDto>> Handle(
        GetEnrollmentPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.EnrollmentRequest)
            .Include(e => e.Participants).ThenInclude(p => p.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
            return NotFound<EnrollmentPaymentSummaryDto>("Enrollment not found.");

        var totalAmount = enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var participantCount = enrollment.Participants.Count;
        var baseShare = participantCount > 0
            ? Math.Round(totalAmount / participantCount, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var succeededCount = enrollment.Participants.Count(p => p.PaymentStatus == PaymentStatus.Succeeded);
        var amountPaid = baseShare * succeededCount;
        if (succeededCount == participantCount && participantCount > 0)
            amountPaid = totalAmount; // last payer absorbed rounding; report exact

        var participantDtos = enrollment.Participants
            .OrderBy(p => p.Id)
            .Select((p, i) =>
            {
                var isLastPending = enrollment.Kind == EnrollmentKind.Group
                                 && p.PaymentStatus == PaymentStatus.Pending
                                 && enrollment.Participants.Count(x => x.PaymentStatus == PaymentStatus.Pending) == 1;
                var share = enrollment.Kind == EnrollmentKind.Individual
                    ? totalAmount
                    : (isLastPending ? totalAmount - (baseShare * succeededCount) : baseShare);

                return new EnrollmentParticipantPaymentSummaryDto
                {
                    ParticipantId = p.Id,
                    StudentId = p.StudentId,
                    StudentName = p.Student?.User != null
                        ? ((p.Student.User.FirstName ?? "") + " " + (p.Student.User.LastName ?? "")).Trim()
                        : null,
                    PaymentStatus = p.PaymentStatus,
                    PaidAt = p.PaidAt,
                    Share = share
                };
            })
            .ToList();

        return Success(entity: new EnrollmentPaymentSummaryDto
        {
            EnrollmentId = enrollment.Id,
            Kind = enrollment.Kind,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            PaymentDeadline = enrollment.PaymentDeadline,
            ActivatedAt = enrollment.ActivatedAt,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            AmountRemaining = totalAmount - amountPaid,
            Currency = _settings.DefaultCurrency,
            Participants = participantDtos
        });
    }
}
