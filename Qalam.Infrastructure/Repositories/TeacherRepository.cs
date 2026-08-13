using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Course;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
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
        var query = ApplyAdminListFilters(_teachers.AsNoTracking(), filters);
        query = ApplyAdminListSort(query, filters.SortBy);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(ProjectToAdminListRow())
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToAdminListItemDto).ToList();
        await EnrichAdminListItemsAsync(items, cancellationToken);

        return new PaginatedResult<AdminTeacherListItemDto>(items, total, filters.PageNumber, filters.PageSize);
    }

    public async Task<List<AdminTeacherListItemDto>?> ExportForAdminAsync(
        AdminTeacherListFilters filters,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyAdminListFilters(_teachers.AsNoTracking(), filters);
        query = ApplyAdminListSort(query, filters.SortBy);

        var total = await query.CountAsync(cancellationToken);
        if (total > maxRows)
            return null;

        var rows = await query
            .Take(maxRows)
            .Select(ProjectToAdminListRow())
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToAdminListItemDto).ToList();
        await EnrichAdminListItemsAsync(items, cancellationToken);
        return items;
    }

    private IQueryable<Teacher> ApplyAdminListFilters(
        IQueryable<Teacher> query,
        AdminTeacherListFilters filters)
    {
        if (filters.Status.HasValue)
            query = query.Where(t => t.Status == filters.Status.Value);

        if (filters.Location.HasValue)
            query = query.Where(t => t.Location == filters.Location.Value);

        if (filters.SubjectId.HasValue)
        {
            var subjectId = filters.SubjectId.Value;
            query = query.Where(t => t.TeacherSubjects.Any(ts => ts.SubjectId == subjectId));
        }

        if (filters.DomainId.HasValue)
        {
            var domainId = filters.DomainId.Value;
            var submissionTeacherIds = _context.Set<TeacherDomainQuestionSubmission>()
                .AsNoTracking()
                .Where(s => s.Question.DomainId == domainId)
                .Select(s => s.TeacherId);
            query = query.Where(t =>
                t.TeacherSubjects.Any(ts => ts.Subject != null && ts.Subject.DomainId == domainId)
                || submissionTeacherIds.Contains(t.Id));
        }

        if (filters.CreatedFrom.HasValue)
        {
            var from = filters.CreatedFrom.Value;
            query = query.Where(t => t.CreatedAt >= from);
        }

        if (filters.CreatedTo.HasValue)
        {
            // Inclusive end-of-day when caller passes a date-only value.
            var to = filters.CreatedTo.Value;
            query = query.Where(t => t.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim();
            query = query.Where(t => t.User != null && (
                ((t.User.FirstName ?? "") + " " + (t.User.LastName ?? "")).Contains(s) ||
                (t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(s)) ||
                (t.User.Email != null && t.User.Email.Contains(s))));
        }

        if (!string.IsNullOrWhiteSpace(filters.RequirementCode)
            || filters.RequirementStatus.HasValue)
        {
            var code = filters.RequirementCode?.Trim();
            var hasCode = !string.IsNullOrWhiteSpace(code);

            var subQuery = _context.Set<TeacherRegistrationSubmission>()
                .AsNoTracking()
                .Where(s => !hasCode || s.Requirement.Code == code);

            query = filters.RequirementStatus switch
            {
                TeacherRequirementFilterStatus.NotSubmitted when hasCode =>
                    query.Where(t => !subQuery.Any(s => s.TeacherId == t.Id)),
                TeacherRequirementFilterStatus.NotSubmitted =>
                    // Any active required catalog item with no submission for this teacher.
                    query.Where(t => _context.Set<TeacherRegistrationRequirement>()
                        .AsNoTracking()
                        .Any(r => r.IsActive && r.IsRequired
                            && !_context.Set<TeacherRegistrationSubmission>()
                                .AsNoTracking()
                                .Any(s => s.TeacherId == t.Id && s.RequirementId == r.Id))),
                TeacherRequirementFilterStatus.Submitted =>
                    query.Where(t => subQuery.Any(s => s.TeacherId == t.Id)),
                TeacherRequirementFilterStatus.Pending =>
                    query.Where(t => subQuery.Any(s =>
                        s.TeacherId == t.Id && s.VerificationStatus == DocumentVerificationStatus.Pending)),
                TeacherRequirementFilterStatus.Approved =>
                    query.Where(t => subQuery.Any(s =>
                        s.TeacherId == t.Id && s.VerificationStatus == DocumentVerificationStatus.Approved)),
                TeacherRequirementFilterStatus.Rejected =>
                    query.Where(t => subQuery.Any(s =>
                        s.TeacherId == t.Id && s.VerificationStatus == DocumentVerificationStatus.Rejected)),
                _ when hasCode =>
                    query.Where(t => subQuery.Any(s => s.TeacherId == t.Id)),
                _ => query,
            };
        }

        return query;
    }

    private static IQueryable<Teacher> ApplyAdminListSort(
        IQueryable<Teacher> query,
        AdminTeacherListSort sortBy) =>
        sortBy switch
        {
            AdminTeacherListSort.NameAsc => query
                .OrderBy(t => t.User!.FirstName)
                .ThenBy(t => t.User!.LastName),
            AdminTeacherListSort.Status => query
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

    private async Task EnrichAdminListItemsAsync(
        List<AdminTeacherListItemDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        var teacherIds = items.Select(i => i.TeacherId).ToList();

        var subjectRows = await _context.Set<TeacherSubject>()
            .AsNoTracking()
            .Where(ts => teacherIds.Contains(ts.TeacherId) && ts.Subject != null)
            .Select(ts => new
            {
                ts.TeacherId,
                SubjectNameAr = ts.Subject!.NameAr,
                SubjectNameEn = ts.Subject.NameEn,
                DomainId = ts.Subject.DomainId,
                DomainCode = ts.Subject.Domain != null ? ts.Subject.Domain.Code : null,
                DomainNameAr = ts.Subject.Domain != null ? ts.Subject.Domain.NameAr : null,
                DomainNameEn = ts.Subject.Domain != null ? ts.Subject.Domain.NameEn : null,
            })
            .ToListAsync(cancellationToken);

        var certRows = await _context.Set<TeacherDocument>()
            .AsNoTracking()
            .Where(d => teacherIds.Contains(d.TeacherId) && d.DocumentType == TeacherDocumentType.Certificate)
            .Select(d => new
            {
                d.TeacherId,
                Title = d.CertificateTitle ?? d.Issuer ?? "",
            })
            .ToListAsync(cancellationToken);

        var submissions = await _context.Set<TeacherDomainQuestionSubmission>()
            .AsNoTracking()
            .Include(s => s.Question).ThenInclude(q => q.Domain)
            .Include(s => s.Documents)
            .Where(s => teacherIds.Contains(s.TeacherId))
            .ToListAsync(cancellationToken);

        var subjectsByTeacher = subjectRows.GroupBy(r => r.TeacherId).ToDictionary(g => g.Key, g => g.ToList());
        var certsByTeacher = certRows.GroupBy(r => r.TeacherId).ToDictionary(g => g.Key, g => g.ToList());
        var submissionsByTeacher = submissions.GroupBy(s => s.TeacherId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in items)
        {
            subjectsByTeacher.TryGetValue(item.TeacherId, out var subjects);
            certsByTeacher.TryGetValue(item.TeacherId, out var certs);
            submissionsByTeacher.TryGetValue(item.TeacherId, out var subs);

            subjects ??= [];
            certs ??= [];
            subs ??= [];

            var groups = BuildDomainQuestionGroups(subs);
            item.DomainQuestionSubmissions = groups;

            var domainMap = new Dictionary<int, (string Code, string NameAr, string NameEn)>();
            foreach (var g in groups)
            {
                domainMap[g.DomainId] = (g.DomainCode, g.DomainNameAr, g.DomainNameEn);
            }

            foreach (var s in subjects)
            {
                if (s.DomainId <= 0 || string.IsNullOrWhiteSpace(s.DomainCode))
                    continue;
                domainMap.TryAdd(
                    s.DomainId,
                    (s.DomainCode!, s.DomainNameAr ?? "", s.DomainNameEn ?? ""));
            }

            item.SelectedDomainCodes = string.Join("; ", domainMap.Values.Select(d => d.Code).Distinct());
            item.SelectedDomainNamesAr = string.Join("; ", domainMap.Values.Select(d => d.NameAr).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
            item.SelectedDomainNamesEn = string.Join("; ", domainMap.Values.Select(d => d.NameEn).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
            item.SubjectNamesAr = string.Join("; ", subjects.Select(s => s.SubjectNameAr).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
            item.SubjectNamesEn = string.Join("; ", subjects.Select(s => s.SubjectNameEn).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
            item.CertificateTitles = string.Join("; ", certs.Select(c => c.Title).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct());
            item.DomainAnswersSummary = AdminTeacherCsvHelper.BuildDomainAnswersSummary(groups);
        }
    }

    private static List<TeacherDomainQuestionGroupDto> BuildDomainQuestionGroups(
        List<TeacherDomainQuestionSubmission> submissions)
    {
        if (submissions.Count == 0)
            return [];

        // Latest submission per question.
        var latest = submissions
            .GroupBy(s => s.QuestionId)
            .Select(g => g.OrderByDescending(s => s.Id).First())
            .ToList();

        return latest
            .Where(s => s.Question?.Domain != null)
            .GroupBy(s => new
            {
                s.Question.DomainId,
                s.Question.Domain.Code,
                s.Question.Domain.NameAr,
                s.Question.Domain.NameEn
            })
            .Select(g => new TeacherDomainQuestionGroupDto
            {
                DomainId = g.Key.DomainId,
                DomainCode = g.Key.Code,
                DomainNameAr = g.Key.NameAr,
                DomainNameEn = g.Key.NameEn,
                Questions = g.Select(MapDomainSubmissionStatus).OrderBy(q => q.Code).ToList()
            })
            .OrderBy(g => g.DomainNameEn)
            .ToList();
    }

    private static TeacherDomainQuestionSubmissionStatusDto MapDomainSubmissionStatus(
        TeacherDomainQuestionSubmission submission)
    {
        var q = submission.Question;
        List<RequirementOptionDto>? selectedOptions = null;

        if (q.RequirementType == RegistrationRequirementType.Selection
            && !string.IsNullOrWhiteSpace(submission.TextValue))
        {
            var allowed = RegistrationRequirementOptionsHelper.Parse(q.OptionsJson);
            var picked = submission.TextValue.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            selectedOptions = allowed
                .Where(o => picked.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
                .Select(o => new RequirementOptionDto
                {
                    Value = o.Value,
                    LabelAr = o.LabelAr,
                    LabelEn = o.LabelEn
                })
                .ToList();
        }

        var docIds = submission.Documents?
            .Select(d => d.TeacherDocumentId)
            .Distinct()
            .ToList() ?? [];
        if (submission.TeacherDocumentId is int primary && !docIds.Contains(primary))
            docIds.Insert(0, primary);

        return new TeacherDomainQuestionSubmissionStatusDto
        {
            SubmissionId = submission.Id,
            QuestionId = q.Id,
            Code = q.Code,
            NameAr = q.NameAr,
            NameEn = q.NameEn,
            RequirementType = q.RequirementType.ToString(),
            IsRequired = q.IsRequired,
            RequiresAdminReview = q.RequiresAdminReview,
            IsSubmitted = true,
            VerificationStatus = submission.VerificationStatus,
            RejectionReason = submission.RejectionReason,
            TeacherDocumentId = submission.TeacherDocumentId,
            TeacherDocumentIds = docIds,
            TextValue = submission.TextValue,
            BoolValue = submission.BoolValue,
            SelectedOptions = selectedOptions
        };
    }

    public async Task<AdminTeacherStatusSummaryDto> GetStatusSummaryAsync(
        bool includeAwaitingPlatformLaunch,
        CancellationToken cancellationToken = default)
    {
        var groups = await _teachers.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(TeacherStatus status) =>
            groups.FirstOrDefault(g => g.Status == status)?.Count ?? 0;

        var summary = new AdminTeacherStatusSummaryDto
        {
            AwaitingDocuments = CountOf(TeacherStatus.AwaitingDocuments),
            PendingVerification = CountOf(TeacherStatus.PendingVerification),
            DocumentsRejected = CountOf(TeacherStatus.DocumentsRejected),
            Active = CountOf(TeacherStatus.Active),
            Blocked = CountOf(TeacherStatus.Blocked),
        };
        summary.Total = summary.AwaitingDocuments
            + summary.PendingVerification
            + summary.DocumentsRejected
            + summary.Active
            + summary.Blocked;

        if (includeAwaitingPlatformLaunch)
        {
            summary.AwaitingPlatformLaunch = await _teachers.AsNoTracking()
                .CountAsync(
                    t => t.Status == TeacherStatus.Active
                         && t.TeacherSubjects.Any()
                         && t.TeacherAvailabilities.Any(a => a.IsActive),
                    cancellationToken);
        }

        return summary;
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

    public async Task<List<(int TeacherId, string? Email, string? PhoneNumber)>> GetContactInfoByTeacherIdsAsync(
        IReadOnlyCollection<int> teacherIds,
        CancellationToken cancellationToken = default)
    {
        if (teacherIds.Count == 0) return new List<(int, string?, string?)>();

        var rows = await _teachers
            .AsNoTracking()
            .Where(t => teacherIds.Contains(t.Id) && t.User != null)
            .Select(t => new
            {
                t.Id,
                Email = t.User!.Email,
                PhoneNumber = t.User.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Id, (string?)r.Email, (string?)r.PhoneNumber)).ToList();
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

    public async Task<List<TeacherCardDto>> GetRecommendedForDomainsAsync(
        IReadOnlyCollection<int> domainIds,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveTeachersBaseQuery();

        if (domainIds.Count > 0)
        {
            var domains = domainIds.ToList();
            query = query.Where(t => t.TeacherSubjects.Any(ts =>
                ts.IsActive && ts.Subject != null && domains.Contains(ts.Subject.DomainId)));
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
                ts.IsActive
                && (!qContent.HasValue
                    || !ts.QuranContentTypes.Any()
                    || ts.QuranContentTypes.Any(c => c.QuranContentTypeId == qContent.Value))
                && (!qLevel.HasValue
                    || !ts.QuranLevels.Any()
                    || ts.QuranLevels.Any(l => l.QuranLevelId == qLevel.Value))));
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
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var previewLimit = limit is < 1 or > 20 ? 10 : limit;
        var activeStatuses = new[] { EnrollmentStatus.Active, EnrollmentStatus.Completed };
        var isAr = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

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
                SubjectsCount = t.TeacherSubjects.Count(ts => ts.IsActive),
                SessionsCount = _context.Set<Qalam.Data.Entity.Course.CourseSchedule>()
                    .Count(s => s.Enrollment.ApprovedByTeacherId == teacherId
                                && s.Status == ScheduleStatus.Completed),
                Subjects = t.TeacherSubjects
                    .Where(ts => ts.IsActive
                                 && ts.Subject != null)
                    .OrderBy(ts => ts.Subject!.NameAr)
                    .Take(previewLimit)
                    .Select(ts => new TeacherCardSubjectDto
                    {
                        SubjectId = ts.SubjectId,
                        SubjectNameAr = ts.Subject!.NameAr,
                        SubjectNameEn = ts.Subject.NameEn,
                        DomainId = ts.Subject.DomainId,
                        DomainCode = ts.Subject.Domain != null ? ts.Subject.Domain.Code : null,
                        GradeNameAr = ts.Subject.Grade != null ? ts.Subject.Grade.NameAr : null,
                        GradeNameEn = ts.Subject.Grade != null ? ts.Subject.Grade.NameEn : null,
                        CanTeachFullSubject = ts.CanTeachFullSubject,
                        UnitsCount = ts.TeacherSubjectUnits.Count
                    })
                    .ToList(),
                Reviews = t.TeacherReviews
                    .Where(r => r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(previewLimit)
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
                    .ToList(),
                Certificates = t.TeacherDocuments
                    .Where(d => d.DocumentType == TeacherDocumentType.Certificate
                                && d.VerificationStatus == DocumentVerificationStatus.Approved)
                    .OrderByDescending(d => d.IssueDate ?? DateOnly.MinValue)
                    .Take(previewLimit)
                    .Select(d => new StudentTeacherCertificateDto
                    {
                        Id = d.Id,
                        Title = d.CertificateTitle,
                        Issuer = d.Issuer,
                        IssueDate = d.IssueDate,
                        FileUrl = d.FilePath
                    })
                    .ToList(),
                Courses = _context.Set<Qalam.Data.Entity.Course.Course>()
                    .Where(c => c.TeacherId == teacherId
                                && c.Status == CourseStatus.Published
                                && c.IsActive)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(previewLimit)
                    .Select(c => new CourseCatalogIndexItemDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        ImageUrl = c.ImageUrl,
                        Price = c.Price,
                        TeacherDisplayName = c.Teacher != null && c.Teacher.User != null
                            ? (c.Teacher.User.FirstName + " " + c.Teacher.User.LastName).Trim()
                            : null,
                        TeacherAverageReview = c.Teacher != null
                            ? (c.Teacher.TeacherReviews
                                  .Where(r => r.IsApproved)
                                  .Select(r => (decimal?)r.Rating)
                                  .Average() ?? 0m)
                            : 0m,
                        DomainName = c.TeacherSubject != null &&
                                     c.TeacherSubject.Subject != null &&
                                     c.TeacherSubject.Subject.Domain != null
                            ? (isAr
                                ? c.TeacherSubject.Subject.Domain.NameAr
                                : c.TeacherSubject.Subject.Domain.NameEn)
                            : null,
                        SubjectName = c.TeacherSubject != null && c.TeacherSubject.Subject != null
                            ? (isAr
                                ? c.TeacherSubject.Subject.NameAr
                                : c.TeacherSubject.Subject.NameEn)
                            : null,
                        CurriculumName = c.TeacherSubject != null &&
                                         c.TeacherSubject.Subject != null &&
                                         c.TeacherSubject.Subject.Curriculum != null
                            ? (isAr
                                ? c.TeacherSubject.Subject.Curriculum.NameAr
                                : c.TeacherSubject.Subject.Curriculum.NameEn)
                            : null,
                        LevelName = c.TeacherSubject != null &&
                                    c.TeacherSubject.Subject != null &&
                                    c.TeacherSubject.Subject.Level != null
                            ? (isAr
                                ? c.TeacherSubject.Subject.Level.NameAr
                                : c.TeacherSubject.Subject.Level.NameEn)
                            : null,
                        GradeName = c.TeacherSubject != null &&
                                    c.TeacherSubject.Subject != null &&
                                    c.TeacherSubject.Subject.Grade != null
                            ? (isAr
                                ? c.TeacherSubject.Subject.Grade.NameAr
                                : c.TeacherSubject.Subject.Grade.NameEn)
                            : null,
                        TeachingModeName = c.TeachingMode != null
                            ? (isAr ? c.TeachingMode.NameAr : c.TeachingMode.NameEn)
                            : null,
                        SessionTypeName = c.SessionType != null
                            ? (isAr ? c.SessionType.NameAr : c.SessionType.NameEn)
                            : null,
                        SessionsCount = c.IsFlexible ? null : c.Sessions.Count,
                        SessionDurationMinutes = c.SessionDurationMinutes,
                        TotalDurationMinutes = !c.IsFlexible && c.SessionDurationMinutes.HasValue
                            ? (int?)(c.Sessions.Count * c.SessionDurationMinutes.Value)
                            : null
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

    public async Task<(int StudentsCount, int SessionsCount)> GetMyProfileStatsAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[] { EnrollmentStatus.Active, EnrollmentStatus.Completed };

        var studentsCount = await _context.Set<Qalam.Data.Entity.Course.EnrollmentParticipant>()
            .AsNoTracking()
            .Where(p => activeStatuses.Contains(p.Enrollment.EnrollmentStatus)
                        && p.Enrollment.ApprovedByTeacherId == teacherId)
            .Select(p => p.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var sessionsCount = await _context.Set<Qalam.Data.Entity.Course.CourseSchedule>()
            .AsNoTracking()
            .CountAsync(s => s.Enrollment.ApprovedByTeacherId == teacherId
                             && s.Status == ScheduleStatus.Completed, cancellationToken);

        return (studentsCount, sessionsCount);
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
                    GradeNameAr = ts.Subject.Grade != null ? ts.Subject.Grade.NameAr : null,
                    GradeNameEn = ts.Subject.Grade != null ? ts.Subject.Grade.NameEn : null,
                    CanTeachFullSubject = ts.CanTeachFullSubject,
                    UnitsCount = ts.TeacherSubjectUnits.Count
                })
                .ToList()
        };
}
