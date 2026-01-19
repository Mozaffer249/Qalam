namespace Qalam.Data.DTOs.Teacher;

public class PhoneVerificationDto
{
    public int UserId { get; set; }
    public string Token { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
}
