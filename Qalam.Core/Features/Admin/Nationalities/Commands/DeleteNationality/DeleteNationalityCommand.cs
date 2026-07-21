using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.DeleteNationality;

public class DeleteNationalityCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
}
