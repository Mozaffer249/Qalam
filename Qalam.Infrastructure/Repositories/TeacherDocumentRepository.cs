using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDocumentRepository : GenericRepositoryAsync<TeacherDocument>, ITeacherDocumentRepository
{
    private readonly DbSet<TeacherDocument> _teacherDocuments;

    public TeacherDocumentRepository(ApplicationDBContext context) : base(context)
    {
        _teacherDocuments = context.Set<TeacherDocument>();
    }

    public async Task<bool> IsIdentityNumberUniqueAsync(
        IdentityType type,
        string number,
        string? countryCode,
        int? excludeTeacherId = null)
    {
        var query = _teacherDocuments
            .Where(d => d.IdentityType == type
                     && d.DocumentNumber == number
                     && d.IssuingCountryCode == countryCode);

        if (excludeTeacherId.HasValue)
            query = query.Where(d => d.TeacherId != excludeTeacherId.Value);

        var exists = await query.AnyAsync();
        return !exists;
    }

    public async Task<int> GetCertificateCountAsync(int teacherId)
    {
        return await _teacherDocuments
            .Where(d => d.TeacherId == teacherId
                     && d.DocumentType == TeacherDocumentType.Certificate)
            .CountAsync();
    }

    public async Task<IEnumerable<TeacherDocument>> GetTeacherDocumentsAsync(int teacherId)
    {
        return await _teacherDocuments
            .Where(d => d.TeacherId == teacherId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TeacherDocument>> GetByTeacherIdAsync(int teacherId)
    {
        return await _teacherDocuments
            .Where(d => d.TeacherId == teacherId)
            .ToListAsync();
    }

    public async Task<List<TeacherDocumentReviewDto>> GetDocumentsStatusAsync(int teacherId)
    {
        return await _teacherDocuments
            .Where(d => d.TeacherId == teacherId)
            .OrderBy(d => d.DocumentType)
            .ThenByDescending(d => d.CreatedAt)
            .Select(d => new TeacherDocumentReviewDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                FilePath = d.FilePath,
                VerificationStatus = d.VerificationStatus,
                RejectionReason = d.RejectionReason,
                ReviewedAt = d.ReviewedAt,
                DocumentNumber = d.DocumentNumber,
                IdentityType = d.IdentityType,
                IssuingCountryCode = d.IssuingCountryCode,
                CertificateTitle = d.CertificateTitle,
                Issuer = d.Issuer,
                IssueDate = d.IssueDate,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<RejectedDocumentInfo>> GetRejectedDocumentsAsync(int teacherId)
    {
        return await _teacherDocuments
            .Where(d => d.TeacherId == teacherId
                     && d.VerificationStatus == DocumentVerificationStatus.Rejected)
            .Select(d => new RejectedDocumentInfo
            {
                DocumentId = d.Id,
                DocumentType = d.DocumentType,
                RejectionReason = d.RejectionReason ?? "No reason provided"
            })
            .ToListAsync();
    }

    public Task<int> DeletePendingForTeacherAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _teacherDocuments
            .Where(d => d.TeacherId == teacherId
                     && d.VerificationStatus == DocumentVerificationStatus.Pending)
            .ExecuteDeleteAsync(cancellationToken);
}
