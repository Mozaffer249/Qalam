using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherProfile;

public class GetStudentTeacherProfileQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherProfileQuery, Response<StudentTeacherProfileDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public GetStudentTeacherProfileQueryHandler(
        ITeacherRepository teacherRepository,
        IMediaUrlResolver mediaUrlResolver,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<Response<StudentTeacherProfileDto>> Handle(
        GetStudentTeacherProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _teacherRepository.GetStudentProfileAsync(
            request.TeacherId,
            request.Limit,
            cancellationToken);
        if (profile is null)
            return NotFound<StudentTeacherProfileDto>("Teacher not found.");

        if (!string.IsNullOrWhiteSpace(profile.ProfilePictureUrl))
            profile.ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(profile.ProfilePictureUrl);

        foreach (var course in profile.Courses)
            course.ImageUrl = _mediaUrlResolver.ToPublicUrl(course.ImageUrl);

        foreach (var cert in profile.Certificates)
            cert.FileUrl = _mediaUrlResolver.ToPublicUrl(cert.FileUrl) ?? cert.FileUrl;

        return Success(entity: profile);
    }
}
