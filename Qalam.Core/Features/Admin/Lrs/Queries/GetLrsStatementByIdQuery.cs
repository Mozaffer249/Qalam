using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Lrs.Queries;

public class GetLrsStatementByIdQuery : IRequest<Response<AdminLrsStatementDetailDto>>
{
    public int Id { get; set; }
}
