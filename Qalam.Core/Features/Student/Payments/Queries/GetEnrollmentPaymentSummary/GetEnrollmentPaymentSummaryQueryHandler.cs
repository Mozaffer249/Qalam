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
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly PaymentSettings _settings;

    public GetEnrollmentPaymentSummaryQueryHandler(
        ICourseEnrollmentRepository enrollmentRepository,
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
            .Include(e => e.CourseEnrollmentPayments)
                .ThenInclude(cep => cep.Payment)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
            return NotFound<EnrollmentPaymentSummaryDto>("Enrollment not found.");

        var amountDue = enrollment.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var amountPaid = enrollment.CourseEnrollmentPayments
            .Where(cep => cep.Status == PaymentStatus.Succeeded && cep.Payment != null)
            .Sum(cep => cep.Payment.TotalAmount);

        var paymentStatus = amountPaid >= amountDue && amountDue > 0
            ? PaymentStatus.Succeeded
            : PaymentStatus.Pending;

        return Success(entity: new EnrollmentPaymentSummaryDto
        {
            EnrollmentId = enrollment.Id,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            PaymentStatus = paymentStatus,
            PaymentDeadline = enrollment.PaymentDeadline,
            Currency = _settings.DefaultCurrency
        });
    }
}
