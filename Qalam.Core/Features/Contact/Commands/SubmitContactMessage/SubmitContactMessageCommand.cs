using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommand : IRequest<Response<string>>
{
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string Message { get; set; } = null!;
}
