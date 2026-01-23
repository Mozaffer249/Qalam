using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherRepository : GenericRepositoryAsync<Teacher>, ITeacherRepository
{
    private readonly DbSet<Teacher> _teachers;
    private readonly ApplicationDBContext _context;

    public TeacherRepository(ApplicationDBContext context) : base(context)
    {
        _teachers = context.Set<Teacher>();
        _context = context;
    }

    public async Task<Teacher?> GetByUserIdAsync(int userId)
    {
        return await _teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);
    }

    public async Task UpdateStatusAsync(int teacherId, TeacherStatus status)
    {
        var teacher = await _teachers.FindAsync(teacherId);
        if (teacher != null)
        {
            teacher.Status = status;
            _teachers.Update(teacher);
        }
    }

    public async Task UpdateLocationAsync(int teacherId, TeacherLocation location)
    {
        var teacher = await _teachers.FindAsync(teacherId);
        if (teacher != null)
        {
            teacher.Location = location;
            _teachers.Update(teacher);
        }
    }

    public IQueryable<Teacher> GetPendingTeachersQueryable()
    {
        return _teachers
            .Include(t => t.User)
            .Include(t => t.TeacherDocuments)
            .Where(t => t.Status == TeacherStatus.PendingVerification 
                     || t.Status == TeacherStatus.DocumentsRejected)
            .OrderByDescending(t => t.CreatedAt);
    }

    public async Task<int> CountAsync(IQueryable<Teacher> query)
    {
        return await query.CountAsync();
    }

    public async Task<List<PendingTeacherDto>> GetPendingTeachersDtoAsync(int pageNumber, int pageSize)
    {
        return await _teachers
            .Include(t => t.User)
            .Include(t => t.TeacherDocuments)
            .Where(t => t.Status == TeacherStatus.PendingVerification 
                     || t.Status == TeacherStatus.DocumentsRejected)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PendingTeacherDto
            {
                TeacherId = t.Id,
                UserId = t.UserId ?? 0,
                FullName = t.User != null 
                    ? (t.User.FirstName ?? "") + " " + (t.User.LastName ?? "") 
                    : "Unknown",
                PhoneNumber = t.User != null ? t.User.PhoneNumber ?? "" : "",
                Email = t.User != null ? t.User.Email : null,
                Status = t.Status,
                Location = t.Location,
                CreatedAt = t.CreatedAt,
                TotalDocuments = t.TeacherDocuments.Count,
                PendingDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Pending),
                ApprovedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Approved),
                RejectedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Rejected)
            })
            .ToListAsync();
    }

    public async Task<TeacherDetailsDto?> GetTeacherDetailsAsync(int teacherId)
    {
        return await _teachers
            .Include(t => t.User)
            .Include(t => t.TeacherDocuments)
            .Where(t => t.Id == teacherId)
            .Select(t => new TeacherDetailsDto
            {
                TeacherId = t.Id,
                UserId = t.UserId ?? 0,
                FullName = t.User != null 
                    ? (t.User.FirstName ?? "") + " " + (t.User.LastName ?? "") 
                    : "Unknown",
                PhoneNumber = t.User != null ? t.User.PhoneNumber ?? "" : "",
                Email = t.User != null ? t.User.Email : null,
                Bio = t.Bio,
                Status = t.Status,
                Location = t.Location,
                CreatedAt = t.CreatedAt,
                TotalDocuments = t.TeacherDocuments.Count,
                PendingDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Pending),
                ApprovedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Approved),
                RejectedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Rejected),
                Documents = t.TeacherDocuments.Select(d => new TeacherDocumentReviewDto
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
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
