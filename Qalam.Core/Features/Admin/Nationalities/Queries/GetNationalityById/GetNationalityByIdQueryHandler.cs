using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Queries.GetNationalityById;

public class GetNationalityByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetNationalityByIdQuery, Response<NationalityAdminDto>>
{
    private readonly INationalityRepository _repository;
    private readonly INationalityProvider _provider;

    public GetNationalityByIdQueryHandler(
        INationalityRepository repository,
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<NationalityAdminDto>> Handle(
        GetNationalityByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<NationalityAdminDto>("Nationality not found");

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
