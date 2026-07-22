using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Colleges.Queries.GetCollegesList;

public class GetCollegesListQuery : IRequest<Response<List<CollegeDto>>>
{
    public int? UniversityId { get; set; }
    public bool ActiveOnly { get; set; }
}
