using System.Threading.Tasks;

namespace Qalam.Service.Abstracts
{
    public interface IRiskAssessmentService
    {
        Task<RiskAssessment> AssessLoginRiskAsync(string ipAddress, int? userId);
        Task RecordLoginAttemptAsync(string ipAddress, int? userId, string? userName, bool wasSuccessful);
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task BlockIpAsync(string ipAddress, int durationMinutes);
    }

    public class RiskAssessment
    {
        public RiskLevel Level { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int FailedAttemptsInWindow { get; set; }
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}

