using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

public class TeacherBalanceAdjustment : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public TeacherBalanceAdjustmentKind Kind { get; set; }

    public TeacherBalanceAdjustmentStatus Status { get; set; } = TeacherBalanceAdjustmentStatus.Pending;

    [Required, MaxLength(64)]
    public string ReasonCode { get; set; } = "";

    [MaxLength(500)]
    public string ReasonText { get; set; } = "";

    public int? RelatedRefundId { get; set; }

    public int? RelatedEarningLineId { get; set; }

    public int? RelatedComplaintId { get; set; }

    public int? CreatedByUserId { get; set; }

    public Teacher.Teacher Teacher { get; set; } = null!;
}
