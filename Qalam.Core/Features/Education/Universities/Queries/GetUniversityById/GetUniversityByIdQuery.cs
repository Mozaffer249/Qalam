using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Universities.Queries.GetUniversityById;

public class GetUniversityByIdQuery : IRequest<Response<UniversityDto>>
{
    public int Id { get; set; }
}
