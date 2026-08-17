using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetWritableFilterValueById;

public class GetWritableFilterValueByIdQuery : IRequest<Response<WritableFilterValueDto>>
{
    public int Id { get; set; }
}
