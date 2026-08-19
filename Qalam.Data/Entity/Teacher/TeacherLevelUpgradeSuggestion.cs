using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// System-suggested level upgrade pending admin approval (hybrid progression).
/// </summary>
public class TeacherLevelUpgradeSuggestion : AuditableEntity
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int CurrentLevelId { get; set; }

    public int SuggestedLevelId { get; set; }

    public decimal AvgRating { get; set; }

    public int CompletedSessions { get; set; }

    public decimal AttendanceRate { get; set; }

    public TeacherLevelUpgradeSuggestionStatus Status { get; set; } = TeacherLevelUpgradeSuggestionStatus.Pending;

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(500)]
    public string? ReviewNotes { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public TeacherLevel CurrentLevel { get; set; } = null!;
    public TeacherLevel SuggestedLevel { get; set; } = null!;
}
