using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;

public class GetMyTeacherProfileQueryHandler : ResponseHandler,
    IRequestHandler<GetMyTeacherProfileQuery, Response<TeacherMyProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly UserManager<User> _userManager;

    public GetMyTeacherProfileQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        UserManager<User> userManager) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _userManager = userManager;
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

        return Success(entity: new TeacherMyProfileDto
        {
            TeacherId = teacher.Id,
            UserId = teacher.UserId ?? request.UserId,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user?.UserName ?? "Teacher" : fullName,
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            Email = user?.Email,
            PhoneNumber = user?.PhoneNumber,
            ProfilePictureUrl = user?.ProfilePictureUrl,
            Nationality = user?.Nationality,
            Address = user?.Address,
            Bio = teacher.Bio,
            Location = teacher.Location,
            Status = teacher.Status,
            RatingAverage = teacher.RatingAverage,
            CreatedAt = teacher.CreatedAt,
        });
    }
}
