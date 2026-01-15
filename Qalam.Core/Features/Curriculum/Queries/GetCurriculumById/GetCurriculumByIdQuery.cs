using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Curriculum.Queries.GetCurriculumById;

public class GetCurriculumByIdQuery : IRequest<Response<CurriculumDto>>
{
    public int Id { get; set; }
}
