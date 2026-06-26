namespace Qalam.Service.Abstracts;

public interface ITeacherLifecycleEmailService
{
    Task SendRegistrationReceivedAsync(int teacherId, CancellationToken cancellationToken = default);

    Task SendDocumentRejectedAsync(
        int teacherId,
        string documentLabel,
        string reason,
        CancellationToken cancellationToken = default);

    Task SendSubjectRejectedAsync(
        int teacherId,
        string subjectName,
        string reason,
        CancellationToken cancellationToken = default);

    Task SendDomainVerificationRejectedAsync(
        int teacherId,
        string domainName,
        string reason,
        CancellationToken cancellationToken = default);

    Task SendAccountActivatedAsync(int teacherId, CancellationToken cancellationToken = default);

    Task SendAccountBlockedAsync(int teacherId, string? reason, CancellationToken cancellationToken = default);

    Task SendAccountUnblockedAsync(int teacherId, CancellationToken cancellationToken = default);
}
