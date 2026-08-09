namespace Qalam.Service.Abstracts;

public interface ITeacherDomainApprovalService
{
    Task<(bool Success, string? ErrorMessage)> ApproveDomainAsync(
        int teacherId,
        int domainId,
        int adminId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorMessage)> RevokeAsync(
        int teacherId,
        int domainId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> IsDomainApprovedAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyApprovedDomainAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetApprovedAtAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);
}
