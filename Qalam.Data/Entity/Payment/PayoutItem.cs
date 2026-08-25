using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Payment;

public class PayoutItem : AuditableEntity
{
    public int Id { get; set; }

    public int PayoutBatchId { get; set; }

    public int TeacherId { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public PayoutBatch PayoutBatch { get; set; } = null!;
    public Teacher.Teacher Teacher { get; set; } = null!;
    public ICollection<TeacherEarningLine> EarningLines { get; set; } = new List<TeacherEarningLine>();
}
