using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDocumentRepository : IGenericRepositoryAsync<TeacherDocument>
{
    Task<bool> IsIdentityNumberUniqueAsync(
        IdentityType type,
        string number,
        string? countryCode);

    Task<int> GetCertificateCountAsync(int teacherId);
    Task<IEnumerable<TeacherDocument>> GetTeacherDocumentsAsync(int teacherId);
    
    // Admin operations
    Task<List<TeacherDocument>> GetByTeacherIdAsync(int teacherId);
    Task<List<TeacherDocumentReviewDto>> GetDocumentsStatusAsync(int teacherId);
    
    // Teacher operations
    Task<List<RejectedDocumentInfo>> GetRejectedDocumentsAsync(int teacherId);
}
