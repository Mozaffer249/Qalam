namespace Qalam.Data.DTOs.Teacher;

public class RegistrationStepDto
{
    public int CurrentStep { get; set; }
    public int NextStep { get; set; }
    public string NextStepName { get; set; } = string.Empty;
    public bool IsRegistrationComplete { get; set; }
        public string? Message { get; set; }
}
