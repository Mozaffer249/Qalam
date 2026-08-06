using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.TeachingPreferences.Commands.UpdateTeacherTeachingPreferences;

public class UpdateTeacherTeachingPreferencesCommand : IRequest<Response<TeacherTeachingPreferencesDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public bool OffersOnline { get; set; }
    public bool OffersInPerson { get; set; }
    public bool OffersIndividual { get; set; }
    public bool OffersGroup { get; set; }
    public string? JobTitle { get; set; }
    public int YearsOfExperience { get; set; }
}
