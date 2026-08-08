namespace Qalam.Data.DTOs.Admin;

public class AdminContactMessageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string Reason { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? ClosedByAdminUserId { get; set; }
}

public class CloseContactMessageRequest
{
    public string? AdminNote { get; set; }
}
