using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Queries.GetStudentIdentity;

public class GetStudentIdentityQuery : IRequest<Response<StudentIdentityResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
