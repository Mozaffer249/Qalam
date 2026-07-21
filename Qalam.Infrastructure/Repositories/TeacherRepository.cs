using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;
using StudentEntity = Qalam.Data.Entity.Student.Student;

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
                Nationality = t.User != null ? t.User.Nationality : null,
                CreatedAt = t.CreatedAt,
                TotalDocuments = t.TeacherDocuments.Count,
                PendingDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Pending),
                ApprovedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Approved),
                RejectedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Rejected),
                PendingDomainQuestions = _context.Set<TeacherDomainQuestionSubmission>()
                    .Count(s => s.TeacherId == t.Id
                                && s.VerificationStatus == DocumentVerificationStatus.Pending
                                && s.Question.RequiresAdminReview)
            })
            .ToListAsync();
    }

    public async Task<PaginatedResult<AdminTeacherListItemDto>> SearchForAdminAsync(
        AdminTeacherListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = _teachers.AsNoTracking().AsQueryable();

        if (filters.Status.HasValue)
            query = query.Where(t => t.Status == filters.Status.Value);

        if (filters.Location.HasValue)
            query = query.Where(t => t.Location == filters.Location.Value);

        if (filters.SubjectId.HasValue)
        {
            var subjectId = filters.SubjectId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId));
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim();
            query = query.Where(t => t.User != null && (
                ((t.User.FirstName ?? "") + " " + (t.User.LastName ?? "")).Contains(s) ||
                (t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(s)) ||
                (t.User.Email != null && t.User.Email.Contains(s))));
        }

        query = filters.SortBy switch
        {
            AdminTeacherListSort.NameAsc => query
                .OrderBy(t => t.User!.FirstName)
                .ThenBy(t => t.User!.LastName),
            AdminTeacherListSort.Status => query
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(ProjectToAdminListRow())
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToAdminListItemDto).ToList();

        return new PaginatedResult<AdminTeacherListItemDto>(items, total, filters.PageNumber, filters.PageSize);
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
                Nationality = t.User != null ? t.User.Nationality : null,
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

    public async Task<List<(int TeacherId, string Email)>> GetEmailsByTeacherIdsAsync(IReadOnlyCollection<int> teacherIds, CancellationToken cancellationToken = default)
    {
        if (teacherIds.Count == 0) return new List<(int, string)>();

        var rows = await _teachers
            .AsNoTracking()
            .Where(t => teacherIds.Contains(t.Id)
                        && t.User != null
                        && t.User.Email != null
                        && t.User.Email != "")
            .Select(t => new { t.Id, Email = t.User!.Email! })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, r.Email)).ToList();
    }

    public async Task<List<TeacherCardDto>> GetRecommendedForStudentAsync(
        StudentEntity student,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveTeachersBaseQuery();

        if (student.DomainId.HasValue)
        {
            var domainId = student.DomainId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.DomainId == domainId));
        }
        if (student.LevelId.HasValue)
        {
            var levelId = student.LevelId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.LevelId == levelId));
        }
        if (student.GradeId.HasValue)
        {
            var gradeId = student.GradeId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.GradeId == gradeId));
        }

        query = query
            .OrderByDescending(t => t.RatingAverage)
            .ThenByDescending(t => t.TeacherReviews.Count(r => r.IsApproved))
            .ThenByDescending(t => t.CreatedAt);

        return await query.Take(take).Select(ProjectToCard()).ToListAsync(cancellationToken);
    }

    public async Task<PaginatedResult<TeacherCardDto>> SearchAsync(
        TeacherSearchFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveTeachersBaseQuery();

        if (filters.SubjectId.HasValue)
        {
            var subjectId = filters.SubjectId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.SubjectId == subjectId));
        }
        if (filters.DomainId.HasValue)
        {
            var domainId = filters.DomainId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.DomainId == domainId));
        }
        if (filters.LevelId.HasValue)
        {
            var levelId = filters.LevelId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.LevelId == levelId));
        }
        if (filters.GradeId.HasValue)
        {
            var gradeId = filters.GradeId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && ts.Subject.GradeId == gradeId));
        }
        if (filters.QuranContentTypeId.HasValue || filters.QuranLevelId.HasValue)
        {
            var qContent = filters.QuranContentTypeId;
            var qLevel = filters.QuranLevelId;
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.TeacherSubjectUnits.Any(u =>
                    (!qContent.HasValue || u.QuranContentTypeId == qContent.Value) &&
                    (!qLevel.HasValue   || u.QuranLevelId == qLevel.Value))));
        }
        if (filters.Location.HasValue)
        {
            var loc = filters.Location.Value;
            query = query.Where(t => t.Location == loc);
        }
        if (filters.MinRating.HasValue)
        {
            var min = filters.MinRating.Value;
            query = query.Where(t => t.RatingAverage >= min);
        }
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim();
            query = query.Where(t => t.User != null && (
                (t.User.FirstName + " " + t.User.LastName).Contains(s) ||
                (t.Bio != null && t.Bio.Contains(s))));
        }

        query = filters.SortBy switch
        {
            TeacherSearchSort.Newest  => query.OrderByDescending(t => t.CreatedAt),
            TeacherSearchSort.NameAsc => query.OrderBy(t => t.User!.FirstName).ThenBy(t => t.User!.LastName),
            _                         => query.OrderByDescending(t => t.RatingAverage)
                                              .ThenByDescending(t => t.TeacherReviews.Count(r => r.IsApproved))
                                              .ThenByDescending(t => t.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(ProjectToCard())
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TeacherCardDto>(items, total, filters.PageNumber, filters.PageSize);
    }

    public async Task<StudentTeacherProfileDto?> GetStudentProfileAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[] { EnrollmentStatus.Active, EnrollmentStatus.Completed };

        return await ActiveTeachersBaseQuery()
            .Where(t => t.Id == teacherId)
            .Select(t => new StudentTeacherProfileDto
            {
                Id = t.Id,
                UserId = t.UserId,
                FullName = t.User != null
                    ? ((t.User.FirstName ?? string.Empty) + " " + (t.User.LastName ?? string.Empty)).Trim()
                    : string.Empty,
                ProfilePictureUrl = t.User != null ? t.User.ProfilePictureUrl : null,
                Bio = t.Bio,
                RatingAverage = t.RatingAverage,
                ReviewsCount = t.TeacherReviews.Count(r => r.IsApproved),
                Location = t.Location,
                StudentsCount = _context.Set<Qalam.Data.Entity.Course.EnrollmentParticipant>()
                    .Where(p => activeStatuses.Contains(p.Enrollment.EnrollmentStatus)
                                && p.Enrollment.ApprovedByTeacherId == teacherId)
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count(),
                CoursesCount = _context.Set<Qalam.Data.Entity.Course.Course>()
                    .Count(c => c.TeacherId == teacherId
                                && c.Status == CourseStatus.Published
                                && c.IsActive),
                SubjectsCount = t.TeacherSubjects.Count(ts => ts.IsActive
                    && ts.VerificationStatus == DocumentVerificationStatus.Approved),
                Subjects = t.TeacherSubjects
                    .Where(ts => ts.IsActive
                                 && ts.VerificationStatus == DocumentVerificationStatus.Approved
                                 && ts.Subject != null)
                    .OrderBy(ts => ts.Subject!.NameAr)
                    .Take(5)
                    .Select(ts => new TeacherCardSubjectDto
                    {
                        SubjectId = ts.SubjectId,
                        SubjectNameAr = ts.Subject!.NameAr,
                        SubjectNameEn = ts.Subject.NameEn,
                        DomainId = ts.Subject.DomainId,
                        DomainCode = ts.Subject.Domain != null ? ts.Subject.Domain.Code : null,
                        CanTeachFullSubject = ts.CanTeachFullSubject,
                        UnitsCount = ts.TeacherSubjectUnits.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<StudentTeacherReviewDto>> GetStudentReviewsAsync(
        int teacherId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var isActive = await ActiveTeachersBaseQuery()
            .AnyAsync(t => t.Id == teacherId, cancellationToken);
        if (!isActive)
            return new PaginatedResult<StudentTeacherReviewDto>([], 0, pageNumber, pageSize);

        var query = _context.Set<TeacherReview>()
            .AsNoTracking()
            .Where(r => r.TeacherId == teacherId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new StudentTeacherReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Feedback = r.Feedback,
                StudentDisplayName = r.Student != null && r.Student.User != null
                    ? (r.Student.User.FirstName ?? "Student")
                    : "Student",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<StudentTeacherReviewDto>(items, total, pageNumber, pageSize);
    }

    public async Task<List<StudentTeacherCertificateDto>> GetStudentCertificatesAsync(
        int teacherId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var isActive = await ActiveTeachersBaseQuery()
            .AnyAsync(t => t.Id == teacherId, cancellationToken);
        if (!isActive)
            return [];

        var limit = take is < 1 or > 50 ? 10 : take;

        return await _context.Set<TeacherDocument>()
            .AsNoTracking()
            .Where(d => d.TeacherId == teacherId
                        && d.DocumentType == TeacherDocumentType.Certificate
                        && d.VerificationStatus == DocumentVerificationStatus.Approved)
            .OrderByDescending(d => d.IssueDate ?? DateOnly.MinValue)
            .Take(limit)
            .Select(d => new StudentTeacherCertificateDto
            {
                Id = d.Id,
                Title = d.CertificateTitle,
                Issuer = d.Issuer,
                IssueDate = d.IssueDate,
                FileUrl = d.FilePath
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Teacher> ActiveTeachersBaseQuery() =>
        _teachers.AsNoTracking().Where(t => t.Status == TeacherStatus.Active && t.IsActive);

    /// <summary>
    /// Projects a <see cref="Teacher"/> row to a <see cref="TeacherCardDto"/>. Single LINQ-to-SQL —
    /// EF translates the nested <c>Subjects</c> Select into a join + group, no N+1.
    /// </summary>
    private sealed class AdminTeacherListRow
    {
        public int TeacherId { get; init; }
        public int UserId { get; init; }
        public string FullName { get; init; } = null!;
        public string PhoneNumber { get; init; } = null!;
        public string? Email { get; init; }
        public TeacherStatus Status { get; init; }
        public TeacherLocation? Location { get; init; }
        public string? Nationality { get; init; }
        public DateTime CreatedAt { get; init; }
        public int TotalDocuments { get; init; }
        public int PendingDocuments { get; init; }
        public int ApprovedDocuments { get; init; }
        public int RejectedDocuments { get; init; }
    }

    private static System.Linq.Expressions.Expression<Func<Teacher, AdminTeacherListRow>> ProjectToAdminListRow() =>
        t => new AdminTeacherListRow
        {
            TeacherId = t.Id,
            UserId = t.UserId ?? 0,
            FullName = t.User != null
                ? ((t.User.FirstName ?? "") + " " + (t.User.LastName ?? "")).Trim()
                : "Unknown",
            PhoneNumber = t.User != null ? t.User.PhoneNumber ?? "" : "",
            Email = t.User != null ? t.User.Email : null,
            Status = t.Status,
            Location = t.Location,
            Nationality = t.User != null ? t.User.Nationality : null,
            CreatedAt = t.CreatedAt,
            TotalDocuments = t.TeacherDocuments.Count,
            PendingDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Pending),
            ApprovedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Approved),
            RejectedDocuments = t.TeacherDocuments.Count(d => d.VerificationStatus == DocumentVerificationStatus.Rejected)
        };

    private static AdminTeacherListItemDto ToAdminListItemDto(AdminTeacherListRow row) =>
        new()
        {
            TeacherId = row.TeacherId,
            UserId = row.UserId,
            FullName = row.FullName,
            PhoneNumber = row.PhoneNumber,
            Email = row.Email,
            Status = row.Status.ToString(),
            Location = row.Location,
            Nationality = row.Nationality,
            CreatedAt = row.CreatedAt,
            TotalDocuments = row.TotalDocuments,
            PendingDocuments = row.PendingDocuments,
            ApprovedDocuments = row.ApprovedDocuments,
            RejectedDocuments = row.RejectedDocuments
        };

    private static System.Linq.Expressions.Expression<Func<Teacher, TeacherCardDto>> ProjectToCard() =>
        t => new TeacherCardDto
        {
            Id = t.Id,
            UserId = t.UserId,
            FullName = t.User != null
                ? ((t.User.FirstName ?? string.Empty) + " " + (t.User.LastName ?? string.Empty)).Trim()
                : string.Empty,
            ProfilePictureUrl = t.User != null ? t.User.ProfilePictureUrl : null,
            Bio = t.Bio,
            RatingAverage = t.RatingAverage,
            ReviewsCount = t.TeacherReviews.Count(r => r.IsApproved),
            Location = t.Location,
            Subjects = t.TeacherSubjects
                .Where(ts => ts.IsActive && ts.Subject != null)
                .Take(5)
                .Select(ts => new TeacherCardSubjectDto
                {
                    SubjectId = ts.SubjectId,
                    SubjectNameAr = ts.Subject!.NameAr,
                    SubjectNameEn = ts.Subject.NameEn,
                    DomainId = ts.Subject.DomainId,
                    DomainCode = ts.Subject.Domain != null ? ts.Subject.Domain.Code : null,
                    CanTeachFullSubject = ts.CanTeachFullSubject,
                    UnitsCount = ts.TeacherSubjectUnits.Count
                })
                .ToList()
        };
}
