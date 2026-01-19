using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }
    
    public TeacherDocumentsUploadDto Documents { get; set; } = null!;
}
