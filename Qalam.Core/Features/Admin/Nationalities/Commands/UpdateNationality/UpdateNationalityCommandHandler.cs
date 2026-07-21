using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.UpdateNationality;

public class UpdateNationalityCommandHandler : ResponseHandler,
    IRequestHandler<UpdateNationalityCommand, Response<NationalityAdminDto>>
{
    private readonly INationalityRepository _repository;
    private readonly INationalityProvider _provider;

    public UpdateNationalityCommandHandler(
        INationalityRepository repository,
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<NationalityAdminDto>> Handle(
        UpdateNationalityCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<NationalityAdminDto>("Nationality not found");

        var dto = request.Data;
        entity.NameAr = dto.NameAr.Trim();
        entity.NameEn = dto.NameEn.Trim();
        entity.FlagEmoji = string.IsNullOrWhiteSpace(dto.FlagEmoji) ? null : dto.FlagEmoji.Trim();
        entity.IsActive = dto.IsActive;
        entity.SortOrder = dto.SortOrder;

        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Nationality updated", entity: _provider.ToAdminDto(entity));
    }
}
