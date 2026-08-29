namespace Qalam.Service.Abstracts;

public enum ComplaintSessionEarningEffect
{
    None,
    Release,
    Void,
}

public sealed class ComplaintResolutionPlan
{
    public bool IssueRefund { get; init; }
    public decimal RefundAmount { get; init; }
    public int? PaymentId { get; init; }
    public string Currency { get; init; } = "SAR";
    public ComplaintSessionEarningEffect SessionEarningEffect { get; init; }
    public bool CreateReplacementSchedule { get; init; }
    public bool WarnTeacher { get; init; }
}
