using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Payments.Queries.GetMyPayments;

public class GetMyPaymentsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyPaymentsQuery, Response<List<StudentPaymentListItemDto>>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetMyPaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Response<List<StudentPaymentListItemDto>>> Handle(
        GetMyPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var query = _paymentRepository.GetPayerHistoryQuery(request.UserId);
        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = payments.Select(MapListItem).ToList();

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(pageNumber, pageSize, totalCount));
    }

    internal static StudentPaymentListItemDto MapListItem(Data.Entity.Payment.Payment payment)
    {
        var enrollmentItem = payment.PaymentItems
            .FirstOrDefault(i => i.ItemType == PaymentItemType.CourseEnrollment);
        var paidAt = payment.EnrollmentPayments
            .Select(ep => ep.EnrollmentParticipant?.PaidAt)
            .FirstOrDefault(p => p.HasValue)
            ?? (payment.Status == PaymentStatus.Succeeded
                ? payment.UpdatedAt ?? payment.CreatedAt
                : null);

        var refunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount);

        return new StudentPaymentListItemDto
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            TotalAmount = payment.TotalAmount,
            Currency = payment.Currency,
            PaidAt = paidAt,
            Description = enrollmentItem?.Description
                          ?? payment.PaymentItems.FirstOrDefault()?.Description
                          ?? $"Payment #{payment.Id}",
            EnrollmentId = enrollmentItem?.ReferenceId,
            Provider = payment.PaymentProvider,
            RefundedAmount = refunded
        };
    }
}
