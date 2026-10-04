using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminDashboardOverview;

public class GetAdminDashboardOverviewQueryHandler : ResponseHandler,
    IRequestHandler<GetAdminDashboardOverviewQuery, Response<AdminDashboardOverviewDto>>
{
    private readonly IAdminFinanceService _finance;
    private readonly IComplaintService _complaints;

    public GetAdminDashboardOverviewQueryHandler(
        IAdminFinanceService finance,
        IComplaintService complaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _finance = finance;
        _complaints = complaints;
    }

    public async Task<Response<AdminDashboardOverviewDto>> Handle(
        GetAdminDashboardOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var finance = await _finance.GetSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
        var revenue = await _finance.GetRevenueSummaryAsync(request.FromUtc, request.ToUtc, cancellationToken);
        var complaints = await _complaints.GetCountsAsync(filter: null, cancellationToken);

        var overview = new AdminDashboardOverviewDto
        {
            Finance = finance,
            Revenue = revenue,
            Complaints = complaints,
            Currency = finance.Currency,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Alerts = BuildAlerts(finance, complaints)
        };

        return Success(entity: overview);
    }

    private static List<DashboardAlertDto> BuildAlerts(
        AdminFinanceSummaryDto finance,
        Data.DTOs.Complaint.ComplaintCountsDto complaints)
    {
        var alerts = new List<DashboardAlertDto>();

        if (complaints.OpenTotal > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Severity = complaints.DecisionPending > 0 ? "warning" : "info",
                Code = "complaints.open",
                Count = complaints.OpenTotal,
                Href = "/complaints"
            });
        }

        // Approved payout value not yet paid out to teachers.
        if (finance.PayoutsApproved > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Severity = "warning",
                Code = "payouts.awaitingPayment",
                Amount = finance.PayoutsApproved,
                Currency = finance.Currency,
                Href = "/payouts?status=Approved"
            });
        }

        // Teacher earnings accrued but not yet batched into a payout.
        if (finance.TeacherEarningsPending > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Severity = "info",
                Code = "payouts.pendingEarnings",
                Amount = finance.TeacherEarningsPending,
                Currency = finance.Currency,
                Href = "/payouts"
            });
        }

        return alerts;
    }
}
