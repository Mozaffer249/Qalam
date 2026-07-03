using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainsList;

public class GetEducationDomainsListQuery : IRequest<Response<List<EducationDomainTeacherDto>>>, IAuthenticatedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    /// <summary>When true, return only domains where the teacher can add subjects.</summary>
    public bool ForSubjectSelection { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
