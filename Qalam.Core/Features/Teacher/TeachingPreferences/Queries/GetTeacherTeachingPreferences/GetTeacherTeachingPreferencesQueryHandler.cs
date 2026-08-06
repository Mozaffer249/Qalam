using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.TeachingPreferences.Queries.GetTeacherTeachingPreferences;

public class GetTeacherTeachingPreferencesQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherTeachingPreferencesQuery, Response<TeacherTeachingPreferencesDto>>
{
    private readonly ITeacherRepository _teacherRepository;

    public GetTeacherTeachingPreferencesQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<TeacherTeachingPreferencesDto>> Handle(
        GetTeacherTeachingPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherTeachingPreferencesDto>("Teacher not found");

        return Success(entity: new TeacherTeachingPreferencesDto
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
