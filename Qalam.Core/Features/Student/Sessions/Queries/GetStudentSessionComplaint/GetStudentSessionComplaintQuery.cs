using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionComplaint;

public class GetStudentSessionComplaintQuery : IRequest<Response<SessionComplaintDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ComplaintId { get; set; }
}
