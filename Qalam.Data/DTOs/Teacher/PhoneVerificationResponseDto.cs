namespace Qalam.Data.DTOs.Teacher;

public class PhoneVerificationResponseDto
{
    public string Token { get; set; } = null!;
    public RegistrationStepDto NextStep { get; set; } = null!;
}
