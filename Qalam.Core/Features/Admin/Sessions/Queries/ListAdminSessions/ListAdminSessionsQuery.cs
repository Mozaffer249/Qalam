using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.Sessions.Queries.ListAdminSessions;

public class ListAdminSessionsQuery : IRequest<Response<List<AdminSessionListItemDto>>>
{
    public ScheduleStatus? Status { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public int? EnrollmentId { get; set; }
    public bool? HasComplaint { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
