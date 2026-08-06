using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.TeachingPreferences.Commands.UpdateTeacherTeachingPreferences;

public class UpdateTeacherTeachingPreferencesCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherTeachingPreferencesCommand, Response<TeacherTeachingPreferencesDto>>
{
    private readonly ITeacherRepository _teacherRepository;

    public UpdateTeacherTeachingPreferencesCommandHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<TeacherTeachingPreferencesDto>> Handle(
        UpdateTeacherTeachingPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherTeachingPreferencesDto>("Teacher not found");

        teacher.OffersOnline = request.OffersOnline;
        teacher.OffersInPerson = request.OffersInPerson;
        teacher.OffersIndividual = request.OffersIndividual;
        teacher.OffersGroup = request.OffersGroup;
        teacher.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        teacher.YearsOfExperience = request.YearsOfExperience;
        teacher.UpdatedAt = DateTime.UtcNow;

        await _teacherRepository.UpdateAsync(teacher);

        return Success("Teaching preferences updated successfully", entity: new TeacherTeachingPreferencesDto
        {
            OffersOnline = teacher.OffersOnline,
            OffersInPerson = teacher.OffersInPerson,
            OffersIndividual = teacher.OffersIndividual,
            OffersGroup = teacher.OffersGroup,
            JobTitle = teacher.JobTitle,
            YearsOfExperience = teacher.YearsOfExperience
        });
    }
}
