using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class RetryLrsStatementCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
}
