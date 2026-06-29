using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherAvailabilityForAdmin;

public class GetTeacherAvailabilityForAdminQuery : IRequest<Response<TeacherAvailabilityResponseDto>>
{
    public int TeacherId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
