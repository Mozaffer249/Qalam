using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramById;

public class GetAcademicProgramByIdQuery : IRequest<Response<AcademicProgramDto>>
{
    public int Id { get; set; }
}
