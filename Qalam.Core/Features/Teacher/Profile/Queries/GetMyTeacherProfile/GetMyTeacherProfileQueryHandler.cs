using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;

public class GetMyTeacherProfileQueryHandler : ResponseHandler,
    IRequestHandler<GetMyTeacherProfileQuery, Response<TeacherMyProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly UserManager<User> _userManager;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GetMyTeacherProfileQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        UserManager<User> userManager,
        IMediaUrlResolver mediaUrlResolver) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _userManager = userManager;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<Response<TeacherMyProfileDto>> Handle(
        GetMyTeacherProfileQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherMyProfileDto>("Teacher not found");

        var user = teacher.UserId.HasValue
            ? await _userManager.FindByIdAsync(teacher.UserId.Value.ToString())
            : null;

        var firstName = user?.FirstName ?? "";
        var lastName = user?.LastName ?? "";
        var fullName = $"{firstName} {lastName}".Trim();

        var (studentsCount, sessionsCount) = await _teacherRepository.GetMyProfileStatsAsync(
            teacher.Id,
            cancellationToken);

        // Ensure level nav is available for response fields.
        var teacherWithLevel = await _teacherRepository.GetByIdWithLevelAsync(teacher.Id, cancellationToken)
            ?? teacher;

        return Success(entity: new TeacherMyProfileDto
        {
            TeacherId = teacher.Id,
            UserId = teacher.UserId ?? request.UserId,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user?.UserName ?? "Teacher" : fullName,
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            Email = user?.Email,
            PhoneNumber = user?.PhoneNumber,
            ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(user?.ProfilePictureUrl),
            Nationality = user?.Nationality,
            Address = user?.Address,
            Bio = teacher.Bio,
            SampleLessonMediaUrl = _mediaUrlResolver.ToPublicUrl(teacher.SampleLessonMediaPath),
            SampleLessonMediaKind = teacher.SampleLessonMediaKind == 1
                ? "image"
                : teacher.SampleLessonMediaKind == 2
                    ? "video"
                    : null,
            JobTitle = teacher.JobTitle,
            YearsOfExperience = teacher.YearsOfExperience,
            OffersOnline = teacher.OffersOnline,
            OffersInPerson = teacher.OffersInPerson,
            OffersIndividual = teacher.OffersIndividual,
            OffersGroup = teacher.OffersGroup,
            StudentsCount = studentsCount,
            SessionsCount = sessionsCount,
            Location = teacher.Location,
            Status = teacher.Status,
            RatingAverage = teacher.RatingAverage,
            CreatedAt = teacher.CreatedAt,
            TeacherLevelId = teacherWithLevel.TeacherLevelId,
            TeacherLevelCode = teacherWithLevel.TeacherLevel?.Code,
            HasCompletedInterviewSession = teacherWithLevel.HasCompletedInterviewSession,
        });
    }
}
