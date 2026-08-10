using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Student;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class GuardianChildrenService : IGuardianChildrenService
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly IGuardianRepository _guardianRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ICurriculumRepository _curriculumRepository;
    private readonly IEducationLevelRepository _levelRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorage;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GuardianChildrenService(
        IGuardianRepository guardianRepository,
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IEducationDomainRepository domainRepository,
        ICurriculumRepository curriculumRepository,
        IEducationLevelRepository levelRepository,
        IGradeRepository gradeRepository,
        UserManager<User> userManager,
        IFileStorageService fileStorage,
        IMediaUrlResolver mediaUrlResolver)
    {
        _guardianRepository = guardianRepository;
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _domainRepository = domainRepository;
        _curriculumRepository = curriculumRepository;
        _levelRepository = levelRepository;
        _gradeRepository = gradeRepository;
        _userManager = userManager;
        _fileStorage = fileStorage;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<List<ChildStudentDto>?> GetMyChildrenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return null;

        var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
        var childrenDtos = children.Select(MapChild).ToList();

        var selfStudent = await _studentRepository.GetTableNoTracking()
            .Where(s => s.UserId == userId && s.IsActive && s.GuardianId == null)
            .Include(s => s.Domain)
            .Include(s => s.Curriculum)
            .Include(s => s.Level)
            .Include(s => s.Grade)
            .Include(s => s.User)
            .FirstOrDefaultAsync(cancellationToken);

        if (selfStudent != null)
        {
            var selfDto = MapChild(selfStudent);
            selfDto.IsSelf = true;
            childrenDtos.Insert(0, selfDto);
        }

        await EnrichWithSessionAndProgressAsync(childrenDtos, cancellationToken);
        return childrenDtos;
    }

    public async Task<GuardianChildUpdateResult> UpdateChildAsync(
        int userId,
        int studentId,
        UpdateChildDto dto,
        CancellationToken cancellationToken = default)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return GuardianChildUpdateResult.FailNotFound("Guardian profile not found.");

        var existing = await _studentRepository.GetTableNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (existing == null || existing.GuardianId != guardian.Id)
            return GuardianChildUpdateResult.FailNotFound("Child not found.");

        if (dto.DomainId.HasValue)
        {
            var domain = await _domainRepository.GetByIdAsync(dto.DomainId.Value);
            if (domain == null)
                return GuardianChildUpdateResult.Fail("Domain not found.");
        }

        if (dto.CurriculumId.HasValue)
        {
            var curriculum = await _curriculumRepository.GetByIdAsync(dto.CurriculumId.Value);
            if (curriculum == null)
                return GuardianChildUpdateResult.Fail("Curriculum not found.");
        }

        if (dto.LevelId.HasValue)
        {
            var level = await _levelRepository.GetByIdAsync(dto.LevelId.Value);
            if (level == null)
                return GuardianChildUpdateResult.Fail("Level not found.");
        }

        if (dto.GradeId.HasValue)
        {
            var grade = await _gradeRepository.GetByIdAsync(dto.GradeId.Value);
            if (grade == null)
                return GuardianChildUpdateResult.Fail("Grade not found.");
        }

        var tracked = await _studentRepository.GetByIdAsync(studentId);
        if (tracked == null)
            return GuardianChildUpdateResult.FailNotFound("Child not found.");

        var fullName = dto.FullName.Trim();
        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : fullName;
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        var user = await _userManager.FindByIdAsync(tracked.UserId.ToString());
        if (user != null)
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            var updateUser = await _userManager.UpdateAsync(user);
            if (!updateUser.Succeeded)
                return GuardianChildUpdateResult.Fail(
                    string.Join("; ", updateUser.Errors.Select(e => e.Description)));
        }

        tracked.DateOfBirth = dto.DateOfBirth;
        tracked.Gender = dto.Gender;
        tracked.GuardianRelation = dto.GuardianRelation;
        tracked.DomainId = dto.DomainId;
        tracked.CurriculumId = dto.CurriculumId;
        tracked.LevelId = dto.LevelId;
        tracked.GradeId = dto.GradeId;

        await _studentRepository.UpdateAsync(tracked);

        var refreshed = await _studentRepository.GetTableNoTracking()
            .Include(s => s.User)
            .Include(s => s.Domain)
            .Include(s => s.Curriculum)
            .Include(s => s.Level)
            .Include(s => s.Grade)
            .FirstAsync(s => s.Id == studentId, cancellationToken);

        return GuardianChildUpdateResult.Ok(MapChild(refreshed));
    }

    public async Task<GuardianChildUpdateResult> UpdateProfilePictureAsync(
        int userId,
        int studentId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return GuardianChildUpdateResult.FailNotFound("Guardian profile not found.");

        var student = await _studentRepository.GetTableNoTracking()
            .Include(s => s.User)
            .Include(s => s.Domain)
            .Include(s => s.Curriculum)
            .Include(s => s.Level)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student == null || student.GuardianId != guardian.Id)
            return GuardianChildUpdateResult.FailNotFound("Child not found.");

        if (file == null || file.Length == 0)
            return GuardianChildUpdateResult.Fail("Profile picture file is required.");

        var valid = await _fileStorage.ValidateFileAsync(file, AllowedImageExtensions, MaxImageBytes);
        if (!valid)
            return GuardianChildUpdateResult.Fail(
                "Invalid image. Use jpg, jpeg, png, or webp up to 5 MB.");

        var previousUrl = student.User?.ProfilePictureUrl;
        await _fileStorage.QueueProfilePicUploadAsync(file, student.UserId, previousUrl);

        // Consumer will overwrite ProfilePictureUrl after OSS upload; return current mapped child.
        return GuardianChildUpdateResult.Ok(MapChild(student));
    }

    public async Task<string?> GetProfilePictureValidationErrorAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Profile picture file is required.";

        var valid = await _fileStorage.ValidateFileAsync(file, AllowedImageExtensions, MaxImageBytes);
        return valid
            ? null
            : "Invalid image. Use jpg, jpeg, png, or webp up to 5 MB.";
    }

    public async Task<HashSet<int>> GetOwnedStudentIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var ownedStudentIds = new HashSet<int>();
        var ownStudent = await _studentRepository.GetByUserIdAsync(userId);
        if (ownStudent != null)
            ownedStudentIds.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return ownedStudentIds;

        var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
        foreach (var child in children)
            ownedStudentIds.Add(child.Id);

        return ownedStudentIds;
    }

    public async Task<int?> ResolveTargetStudentIdAsync(
        int userId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId is int requestedId)
        {
            var owned = await GetOwnedStudentIdsAsync(userId, cancellationToken);
            return owned.Contains(requestedId) ? requestedId : null;
        }

        var self = await _studentRepository.GetByUserIdAsync(userId);
        return self?.Id;
    }

    private ChildStudentDto MapChild(Student student)
    {
        var fullName = student.User != null
            ? string.Join(
                " ",
                new[]
                {
                    (student.User.FirstName ?? "").Trim(),
                    (student.User.LastName ?? "").Trim(),
                }.Where(s => !string.IsNullOrEmpty(s)))
            : "";

        return new ChildStudentDto
        {
            Id = student.Id,
            FullName = fullName,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            GuardianRelation = student.GuardianRelation,
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
            IsActive = student.IsActive,
            ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(student.User?.ProfilePictureUrl),
        };
    }

    private async Task EnrichWithSessionAndProgressAsync(
        List<ChildStudentDto> childrenDtos,
        CancellationToken cancellationToken)
    {
        if (childrenDtos.Count == 0)
            return;

        var studentIds = childrenDtos.Select(c => c.Id).ToList();
        var utcNow = DateTime.UtcNow;

        var enrollments = await _enrollmentRepository.GetTableNoTracking()
            .AsSplitQuery()
            .Where(e => e.Participants.Any(p => studentIds.Contains(p.StudentId)))
            .Include(e => e.Participants)
            .Include(e => e.Course)
                .ThenInclude(c => c!.Sessions)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.TeacherAvailability)
                    .ThenInclude(ta => ta!.TimeSlot)
            .ToListAsync(cancellationToken);

        var byStudent = new Dictionary<int, List<Enrollment>>();
        foreach (var enrollment in enrollments)
        {
            foreach (var participant in enrollment.Participants)
            {
                if (!studentIds.Contains(participant.StudentId))
                    continue;
                if (!byStudent.TryGetValue(participant.StudentId, out var list))
                {
                    list = [];
                    byStudent[participant.StudentId] = list;
                }
                list.Add(enrollment);
            }
        }

        foreach (var dto in childrenDtos)
        {
            if (!byStudent.TryGetValue(dto.Id, out var studentEnrollments) || studentEnrollments.Count == 0)
                continue;

            var completedTotal = 0;
            var sessionsTotal = 0;
            CourseSchedule? bestNext = null;
            int? bestEnrollmentId = null;
            DateTime? bestStart = null;

            foreach (var enrollment in studentEnrollments)
            {
                var schedules = enrollment.CourseSchedules ?? [];
                var completed = schedules.Count(s => s.Status == ScheduleStatus.Completed);
                completedTotal += completed;

                var planned = enrollment.Course?.SessionsCount
                    ?? enrollment.Course?.Sessions?.Count
                    ?? schedules.Count;
                sessionsTotal += planned;

                var next = EnrollmentScheduleHelper.ResolveNextSchedule(schedules, utcNow);
                if (next == null)
                    continue;

                var start = EnrollmentScheduleHelper.ResolveScheduleStartUtc(next);
                if (bestStart == null || start < bestStart)
                {
                    bestStart = start;
                    bestNext = next;
                    bestEnrollmentId = enrollment.Id;
                }
            }

            dto.CompletedSessionsCount = completedTotal;
            dto.SessionsCount = sessionsTotal;
            dto.ProgressPercent = sessionsTotal > 0
                ? (int)Math.Round(completedTotal * 100.0 / sessionsTotal)
                : null;

            if (bestNext != null)
            {
                dto.NextScheduleId = bestNext.Id;
                dto.NextSessionAt = bestStart;
                dto.NextEnrollmentId = bestEnrollmentId;
            }
        }
    }
}

internal static class EnrollmentScheduleHelper
{
    public static CourseSchedule? ResolveNextSchedule(
        IEnumerable<CourseSchedule> schedules,
        DateTime utcNow)
    {
        var actionable = schedules
            .Where(s => s.Status is ScheduleStatus.InProgress or ScheduleStatus.Scheduled)
            .Select(s => (Schedule: s, Start: ResolveScheduleStartUtc(s)))
            .OrderBy(x => x.Start)
            .ToList();

        if (actionable.Count == 0)
            return null;

        var inProgress = actionable
            .Where(x => x.Schedule.Status == ScheduleStatus.InProgress)
            .OrderBy(x => x.Start)
            .Select(x => x.Schedule)
            .FirstOrDefault();
        if (inProgress != null)
            return inProgress;

        var upcoming = actionable
            .Where(x => x.Start >= utcNow)
            .Select(x => x.Schedule)
            .FirstOrDefault();
        if (upcoming != null)
            return upcoming;

        return actionable[0].Schedule;
    }

    public static DateTime ResolveScheduleStartUtc(CourseSchedule schedule)
    {
        var startTime = schedule.TeacherAvailability?.TimeSlot?.StartTime ?? TimeSpan.Zero;
        return PlatformTime.ToUtc(schedule.Date, startTime);
    }
}
