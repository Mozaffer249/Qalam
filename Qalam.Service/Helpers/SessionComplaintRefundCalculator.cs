using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Helpers;

public static class SessionComplaintRefundCalculator
{
    public static decimal ComputeSessionShare(
        decimal amountDue,
        int sessionDurationMinutes,
        int earnablePackageMinutes)
    {
        if (amountDue <= 0 || sessionDurationMinutes <= 0 || earnablePackageMinutes <= 0)
            return 0m;
        return Math.Round(
            amountDue * sessionDurationMinutes / (decimal)earnablePackageMinutes,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static decimal ResolveRefundAmount(
        SessionComplaintResolution resolution,
        ComplaintSessionFinancialContextDto ctx,
        decimal? adminOverride)
    {
        if (resolution is not (SessionComplaintResolution.FullRefund or SessionComplaintResolution.PartialRefund))
            return 0m;

        var sessionShare = ComputeSessionShare(
            ctx.AmountDue,
            ctx.SessionDurationMinutes,
            ctx.EarnablePackageMinutes);

        var target = resolution == SessionComplaintResolution.FullRefund
            ? sessionShare
            : (adminOverride ?? sessionShare);

        if (target <= 0)
            return 0m;

        return Math.Min(target, ctx.RemainingRefundable);
    }
}
