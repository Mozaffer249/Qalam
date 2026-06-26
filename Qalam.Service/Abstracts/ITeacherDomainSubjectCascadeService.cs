namespace Qalam.Service.Abstracts;

public interface ITeacherDomainSubjectCascadeService
{
    Task RejectSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default);

    Task ApproveSubjectsInDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<bool> IsDomainFullyApprovedForTeacherAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<string?> GetSubjectSaveBlockReasonForDomainAsync(
        int teacherId,
        int domainId,
        string domainNameEn,
        string domainCode,
        CancellationToken cancellationToken = default);
}
