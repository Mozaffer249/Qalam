using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.SetNationalityActive;

public class SetNationalityActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetNationalityActiveCommand, Response<NationalityAdminDto>>
{
    private readonly INationalityRepository _repository;
    private readonly INationalityProvider _provider;

    public SetNationalityActiveCommandHandler(
        INationalityRepository repository,
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<NationalityAdminDto>> Handle(
        SetNationalityActiveCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<NationalityAdminDto>("Nationality not found");

        entity.IsActive = request.Data.IsActive;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
