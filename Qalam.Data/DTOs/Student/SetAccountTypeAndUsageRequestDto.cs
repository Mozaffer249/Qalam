namespace Qalam.Data.DTOs.Student;

/// <summary>
/// Request DTO for SetAccountTypeAndUsage - accepts strings for enums
/// </summary>
public class SetAccountTypeAndUsageRequestDto
{
    /// <summary>
    /// Account type: "Student", "Parent", or "Both"
    /// - Student: Register as a student only (can learn)
    /// - Parent: Register as a parent/guardian only (can add children)
    /// - Both: Register as both student and parent (can learn and add children)
    /// </summary>
    public string AccountType { get; set; } = default!;
    
    /// <summary>
    /// Usage mode (required for Parent and Both): "StudySelf", "AddChildren", or "Both"
    /// - StudySelf: Parent will study/learn themselves
    /// - AddChildren: Parent will only add children (not study)
    /// - Both: Parent will study and add children
    /// </summary>
    public string? UsageMode { get; set; }
    
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? CityOrRegion { get; set; }
    
    /// <summary>
    /// Date of birth - must be 18+ to register
    /// </summary>
    public DateOnly DateOfBirth { get; set; }
}
