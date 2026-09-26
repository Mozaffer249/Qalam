using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Profile.Commands.UpdateTeacherBio;

public class UpdateTeacherBioCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherBioCommand, Response<TeacherMyProfileDto>>
{
    private const int MaxBioLength = 500;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMediator _mediator;

    public UpdateTeacherBioCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        IMediator mediator) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _mediator = mediator;
    }

    public async Task<Response<TeacherMyProfileDto>> Handle(
        UpdateTeacherBioCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<TeacherMyProfileDto>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherMyProfileDto>("Teacher not found");

        var bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        if (bio != null && bio.Length > MaxBioLength)
            return BadRequest<TeacherMyProfileDto>($"Bio must be at most {MaxBioLength} characters.");

        teacher.Bio = bio;
        await _teacherRepository.UpdateAsync(teacher);

        return await _mediator.Send(
            new GetMyTeacherProfileQuery { UserId = request.UserId },
            cancellationToken);
    }
}
