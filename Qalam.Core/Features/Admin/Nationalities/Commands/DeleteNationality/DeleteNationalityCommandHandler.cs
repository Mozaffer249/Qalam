using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.DeleteNationality;

public class DeleteNationalityCommandHandler : ResponseHandler,
    IRequestHandler<DeleteNationalityCommand, Response<string>>
{
    private readonly INationalityRepository _repository;

    public DeleteNationalityCommandHandler(
        INationalityRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<string>> Handle(
        DeleteNationalityCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<string>("Nationality not found");

        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();

        return Success(entity: "Nationality deleted");
    }
}
