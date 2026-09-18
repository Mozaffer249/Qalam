using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Queries.GetStudentActiveDomains;

/// <summary>
/// Active education domains for the student app (Home, Domains tab, filter wizard).
/// </summary>
public class GetStudentActiveDomainsQuery
    : IRequest<Response<List<StudentEducationDomainDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
