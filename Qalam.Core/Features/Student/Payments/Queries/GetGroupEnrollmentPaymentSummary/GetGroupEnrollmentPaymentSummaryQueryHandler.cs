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

namespace Qalam.Core.Features.Student.Payments.Queries.GetGroupEnrollmentPaymentSummary;

public class GetGroupEnrollmentPaymentSummaryQueryHandler : ResponseHandler,
    IRequestHandler<GetGroupEnrollmentPaymentSummaryQuery, Response<GroupEnrollmentPaymentSummaryDto>>
{
    private readonly ICourseGroupEnrollmentRepository _groupRepository;
    private readonly PaymentSettings _settings;

    public GetGroupEnrollmentPaymentSummaryQueryHandler(
        ICourseGroupEnrollmentRepository groupRepository,
        IOptions<PaymentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _groupRepository = groupRepository;
        _settings = settings.Value;
    }

    public async Task<Response<GroupEnrollmentPaymentSummaryDto>> Handle(
        GetGroupEnrollmentPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetTableNoTracking()
            .Include(g => g.EnrollmentRequest)
            .Include(g => g.Members).ThenInclude(m => m.Student).ThenInclude(s => s.User)
            .Include(g => g.Members).ThenInclude(m => m.GroupEnrollmentMemberPayments)
                .ThenInclude(gp => gp.Payment)
            .FirstOrDefaultAsync(g => g.Id == request.GroupEnrollmentId, cancellationToken);

        if (group == null)
            return NotFound<GroupEnrollmentPaymentSummaryDto>("Group enrollment not found.");

        var totalAmount = group.EnrollmentRequest?.EstimatedTotalPrice ?? 0m;
        var memberCount = group.Members.Count;
        var baseShare = memberCount > 0
            ? Math.Round(totalAmount / memberCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var memberDtos = new List<GroupEnrollmentMemberPaymentSummaryDto>();
        decimal amountPaid = 0m;

        foreach (var m in group.Members)
        {
            var paid = m.GroupEnrollmentMemberPayments
                .Where(gp => gp.Status == PaymentStatus.Succeeded && gp.Payment != null)
                .Sum(gp => gp.Payment.TotalAmount);
            amountPaid += paid;

            memberDtos.Add(new GroupEnrollmentMemberPaymentSummaryDto
            {
                StudentId = m.StudentId,
                StudentName = m.Student?.User != null
                    ? (m.Student.User.FirstName + " " + m.Student.User.LastName).Trim()
                    : null,
                PaymentStatus = m.PaymentStatus,
                PaidAt = m.PaidAt,
                Share = paid > 0 ? paid : baseShare
            });
        }

        return Success(entity: new GroupEnrollmentPaymentSummaryDto
        {
            GroupEnrollmentId = group.Id,
            Status = group.Status,
            PaymentDeadline = group.PaymentDeadline,
            ActivatedAt = group.ActivatedAt,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            AmountRemaining = totalAmount - amountPaid,
            Currency = _settings.DefaultCurrency,
            Members = memberDtos
        });
    }
}
