using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherDomainQuestionStatus;

public class GetTeacherDomainQuestionStatusQuery : IRequest<Response<TeacherDomainQuestionStatusResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
