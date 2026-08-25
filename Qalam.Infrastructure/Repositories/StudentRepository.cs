using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Student;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class StudentRepository : GenericRepositoryAsync<Student>, IStudentRepository
{
    private readonly DbSet<Student> _students;

    public StudentRepository(ApplicationDBContext context) : base(context)
    {
        _students = context.Set<Student>();
    }

    public async Task<Student?> GetByUserIdAsync(int userId)
    {
        return await _students
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<List<Student>> GetChildrenByGuardianIdAsync(int guardianId)
    {
        return await _students
            .Where(s => s.GuardianId == guardianId && s.IsActive)
            .Include(s => s.Domain)
            .Include(s => s.Curriculum)
            .Include(s => s.Level)
            .Include(s => s.Grade)
            .Include(s => s.User)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaginatedResult<AdminStudentListItemDto>> SearchForAdminAsync(
        AdminStudentListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = _students.AsNoTracking();

        if (filters.IsMinor.HasValue)
            query = query.Where(s => s.IsMinor == filters.IsMinor.Value);

        if (filters.IsActive.HasValue)
            query = query.Where(s => s.IsActive == filters.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var s = filters.Search.Trim();
            query = query.Where(st => st.User != null && (
                ((st.User.FirstName ?? "") + " " + (st.User.LastName ?? "")).Contains(s) ||
                (st.User.PhoneNumber != null && st.User.PhoneNumber.Contains(s)) ||
                (st.User.Email != null && st.User.Email.Contains(s)) ||
                (st.Guardian != null && st.Guardian.FullName != null && st.Guardian.FullName.Contains(s))));
        }

        query = query.OrderByDescending(st => st.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(st => new AdminStudentListItemDto
            {
                StudentId = st.Id,
                UserId = st.UserId,
                FullName = ((st.User.FirstName ?? "") + " " + (st.User.LastName ?? "")).Trim(),
                Email = st.User.Email,
                PhoneNumber = st.User.PhoneNumber,
                IsMinor = st.IsMinor,
                IsActive = st.IsActive,
                GuardianName = st.Guardian == null
                    ? null
                    : st.Guardian.FullName
                      ?? (st.Guardian.User == null
                          ? null
                          : ((st.Guardian.User.FirstName ?? "") + " " + (st.Guardian.User.LastName ?? "")).Trim()),
                ChildrenCount = st.User.GuardianProfile != null
                    ? st.User.GuardianProfile.Students.Count
                    : 0,
                CreatedAt = st.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<AdminStudentListItemDto>(items, total, filters.PageNumber, filters.PageSize);
    }

    public async Task<AdminStudentDetailDto?> GetAdminDetailAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _students
            .AsNoTracking()
            .Include(s => s.User)
                .ThenInclude(u => u.GuardianProfile!)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(c => c.User)
            .Include(s => s.User)
                .ThenInclude(u => u.GuardianProfile!)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(c => c.Domain)
            .Include(s => s.User)
                .ThenInclude(u => u.GuardianProfile!)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(c => c.Curriculum)
            .Include(s => s.User)
                .ThenInclude(u => u.GuardianProfile!)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(c => c.Level)
            .Include(s => s.User)
                .ThenInclude(u => u.GuardianProfile!)
                    .ThenInclude(g => g.Students)
                        .ThenInclude(c => c.Grade)
            .Include(s => s.Guardian)
                .ThenInclude(g => g!.User)
            .Include(s => s.Domain)
            .Include(s => s.Curriculum)
            .Include(s => s.Level)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student == null)
            return null;

        var fullName = FormatUserName(student.User.FirstName, student.User.LastName);
        var guardianProfile = student.User.GuardianProfile;

        AdminStudentGuardianDto? guardianDto = null;
        if (student.Guardian != null)
        {
            var g = student.Guardian;
            guardianDto = new AdminStudentGuardianDto
            {
                GuardianId = g.Id,
                FullName = !string.IsNullOrWhiteSpace(g.FullName)
                    ? g.FullName
                    : FormatUserName(g.User?.FirstName, g.User?.LastName),
                Phone = !string.IsNullOrWhiteSpace(g.Phone) ? g.Phone : g.User?.PhoneNumber,
                Email = !string.IsNullOrWhiteSpace(g.Email) ? g.Email : g.User?.Email,
                Relation = student.GuardianRelation
            };
        }

        var children = new List<AdminStudentChildDto>();
        if (guardianProfile?.Students != null)
        {
            children = guardianProfile.Students
                .OrderBy(c => c.CreatedAt)
                .Select(c => MapChild(c, guardianProfile.UserId))
                .ToList();
        }

        return new AdminStudentDetailDto
        {
            StudentId = student.Id,
            UserId = student.UserId,
            FullName = fullName,
            Email = student.User.Email,
            PhoneNumber = student.User.PhoneNumber,
            IsMinor = student.IsMinor,
            IsActive = student.IsActive,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            Bio = student.Bio,
            CreatedAt = student.CreatedAt,
            DomainId = student.DomainId,
            DomainNameEn = student.Domain?.NameEn,
            DomainNameAr = student.Domain?.NameAr,
            CurriculumId = student.CurriculumId,
            CurriculumNameEn = student.Curriculum?.NameEn,
            CurriculumNameAr = student.Curriculum?.NameAr,
            LevelId = student.LevelId,
            LevelNameEn = student.Level?.NameEn,
            LevelNameAr = student.Level?.NameAr,
            GradeId = student.GradeId,
            GradeNameEn = student.Grade?.NameEn,
            GradeNameAr = student.Grade?.NameAr,
            Guardian = guardianDto,
            Children = children
        };
    }

    private static AdminStudentChildDto MapChild(Student child, int? guardianUserId)
    {
        return new AdminStudentChildDto
        {
            StudentId = child.Id,
            FullName = FormatUserName(child.User?.FirstName, child.User?.LastName),
            DateOfBirth = child.DateOfBirth,
            Gender = child.Gender,
            GuardianRelation = child.GuardianRelation,
            DomainId = child.DomainId,
            DomainNameEn = child.Domain?.NameEn,
            DomainNameAr = child.Domain?.NameAr,
            CurriculumId = child.CurriculumId,
            CurriculumNameEn = child.Curriculum?.NameEn,
            CurriculumNameAr = child.Curriculum?.NameAr,
            LevelId = child.LevelId,
            LevelNameEn = child.Level?.NameEn,
            LevelNameAr = child.Level?.NameAr,
            GradeId = child.GradeId,
            GradeNameEn = child.Grade?.NameEn,
            GradeNameAr = child.Grade?.NameAr,
            IsActive = child.IsActive,
            IsSelf = guardianUserId.HasValue && child.UserId == guardianUserId.Value
        };
    }

    private static string FormatUserName(string? firstName, string? lastName) =>
        string.Join(
            " ",
            new[] { (firstName ?? "").Trim(), (lastName ?? "").Trim() }
                .Where(part => !string.IsNullOrEmpty(part)));
}
