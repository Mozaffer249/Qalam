namespace Qalam.Data.DTOs.Teacher;

public class TeacherRegistrationSubmitResponseDto
{
    public string Message { get; set; } = null!;
    public RegistrationStepDto NextStep { get; set; } = null!;
}
