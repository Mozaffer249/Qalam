using System.ComponentModel.DataAnnotations;
using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Payment;

public class TeacherDisciplinaryRecord : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public TeacherDisciplinaryKind Kind { get; set; }

    public decimal? Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "SAR";

    public int? ComplaintId { get; set; }

    public int? CourseScheduleId { get; set; }

    [MaxLength(64)]
    public string? ResolutionCode { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }

    public Teacher.Teacher Teacher { get; set; } = null!;
}
