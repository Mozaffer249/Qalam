using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Queries.GetPaymentReceipt;

public class GetPaymentReceiptQueryHandler : ResponseHandler,
    IRequestHandler<GetPaymentReceiptQuery, Response<StudentPaymentReceiptDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentReceiptQueryHandler(
        IPaymentRepository paymentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Response<StudentPaymentReceiptDto>> Handle(
        GetPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PaymentId <= 0)
            return BadRequest<StudentPaymentReceiptDto>("paymentId is required.");

        var payment = await _paymentRepository.GetReceiptAsync(request.PaymentId, cancellationToken);
        if (payment == null || payment.PayerUserId != request.UserId)
            return NotFound<StudentPaymentReceiptDto>("Payment not found.");

        return Success(entity: MapReceipt(payment));
    }

    internal static StudentPaymentReceiptDto MapReceipt(Data.Entity.Payment.Payment payment)
    {
        var enrollmentItem = payment.PaymentItems
            .FirstOrDefault(i => i.ItemType == PaymentItemType.CourseEnrollment);
        var enrollment = payment.EnrollmentPayments
            .Select(ep => ep.EnrollmentParticipant?.Enrollment)
            .FirstOrDefault(e => e != null);

        var paidAt = payment.EnrollmentPayments
            .Select(ep => ep.EnrollmentParticipant?.PaidAt)
            .FirstOrDefault(p => p.HasValue)
            ?? (payment.Status == PaymentStatus.Succeeded
                ? payment.UpdatedAt ?? payment.CreatedAt
                : null);

        var refunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount);

        string? teacherName = null;
        if (enrollment?.Course?.Teacher?.User != null)
        {
            var u = enrollment.Course.Teacher.User;
            teacherName = $"{u.FirstName} {u.LastName}".Trim();
        }
        else if (enrollment?.ApprovedByTeacher?.User != null)
        {
            var u = enrollment.ApprovedByTeacher.User;
            teacherName = $"{u.FirstName} {u.LastName}".Trim();
        }

        return new StudentPaymentReceiptDto
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            TotalAmount = payment.TotalAmount,
            Subtotal = payment.Subtotal,
            VatAmount = payment.VatAmount,
            DiscountAmount = payment.DiscountAmount,
            Currency = payment.Currency,
            PaidAt = paidAt,
            Description = enrollmentItem?.Description
                          ?? payment.PaymentItems.FirstOrDefault()?.Description
                          ?? $"Payment #{payment.Id}",
            EnrollmentId = enrollmentItem?.ReferenceId ?? enrollment?.Id,
            Provider = payment.PaymentProvider,
            ProviderTransactionId = payment.ProviderTransactionId,
            InvoiceNumber = payment.InvoiceNumber,
            RefundedAmount = refunded,
            CourseTitle = enrollment?.Course?.Title,
            TeacherName = string.IsNullOrWhiteSpace(teacherName) ? null : teacherName,
            Items = payment.PaymentItems.Select(i => new PaymentReceiptItemDto
            {
                ItemType = i.ItemType.ToString(),
                ReferenceId = i.ReferenceId,
                Description = i.Description,
                Amount = i.Amount
            }).ToList(),
            Refunds = payment.Refunds
                .OrderByDescending(r => r.Id)
                .Select(r => new PaymentReceiptRefundDto
                {
                    RefundId = r.Id,
                    Amount = r.Amount,
                    Currency = r.Currency,
                    Status = r.Status.ToString(),
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt
                }).ToList()
        };
    }
}
