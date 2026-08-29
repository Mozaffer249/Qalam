using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Sessions.Queries.GetAdminSessionById;

public class GetAdminSessionByIdQuery : IRequest<Response<AdminSessionDetailDto>>
{
    public int Id { get; set; }
}
