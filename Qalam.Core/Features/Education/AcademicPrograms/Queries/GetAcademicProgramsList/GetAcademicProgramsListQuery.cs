using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramsList;

public class GetAcademicProgramsListQuery : IRequest<Response<List<AcademicProgramDto>>>
{
    public int? DepartmentId { get; set; }
    public bool ActiveOnly { get; set; }
}
