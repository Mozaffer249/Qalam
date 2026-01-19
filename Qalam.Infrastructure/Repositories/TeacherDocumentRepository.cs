using Microsoft.EntityFrameworkCore;
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
        string? countryCode)
    {
        var exists = await _teacherDocuments
            .AnyAsync(d => d.IdentityType == type
                        && d.DocumentNumber == number
                        && d.IssuingCountryCode == countryCode);
        
        return !exists;  // Return true if unique (doesn't exist)
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
}
