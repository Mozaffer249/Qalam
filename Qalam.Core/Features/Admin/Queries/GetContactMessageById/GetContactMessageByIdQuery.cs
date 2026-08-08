using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetContactMessageById;

public class GetContactMessageByIdQuery : IRequest<Response<AdminContactMessageDto>>
{
    public int Id { get; set; }
}
