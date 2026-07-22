using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Colleges.Queries.GetCollegeById;

public class GetCollegeByIdQuery : IRequest<Response<CollegeDto>>
{
    public int Id { get; set; }
}
