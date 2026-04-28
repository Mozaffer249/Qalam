using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Availability.Queries.GetTeacherAvailabilityByRange;

public class GetTeacherAvailabilityByRangeQuery : IRequest<Response<TeacherAvailabilityByRangeDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int TeacherId { get; set; }

    /// <summary>Optional. Defaults to today.</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>Optional. Defaults to FromDate + 30 days. Capped server-side to FromDate + 90 days.</summary>
    public DateOnly? ToDate { get; set; }
}
