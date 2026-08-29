using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.Sessions.Commands.FileStudentSessionComplaint;

public class FileStudentSessionComplaintCommand : IRequest<Response<SessionComplaintDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int ScheduleId { get; set; }
    public SessionComplaintReason ReasonCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<IFormFile>? Attachments { get; set; }
}
