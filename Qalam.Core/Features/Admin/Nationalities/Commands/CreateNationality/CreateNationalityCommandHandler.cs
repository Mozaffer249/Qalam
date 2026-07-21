using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Common;
using Qalam.Data.Entity.Common;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.CreateNationality;

public class CreateNationalityCommandHandler : ResponseHandler,
    IRequestHandler<CreateNationalityCommand, Response<NationalityAdminDto>>
{
    private readonly INationalityRepository _repository;
    private readonly INationalityProvider _provider;

    public CreateNationalityCommandHandler(
        INationalityRepository repository,
        INationalityProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<NationalityAdminDto>> Handle(
        CreateNationalityCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repository.CodeExistsAsync(code, cancellationToken: cancellationToken))
            return BadRequest<NationalityAdminDto>("Code already exists.");

        var flag = string.IsNullOrWhiteSpace(dto.FlagEmoji)
            ? FlagEmojiHelper.FromIso2(code)
            : dto.FlagEmoji.Trim();

        var entity = new Nationality
        {
            Code = code,
            NameAr = dto.NameAr.Trim(),
            NameEn = dto.NameEn.Trim(),
            FlagEmoji = string.IsNullOrEmpty(flag) ? null : flag,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Nationality created", entity: _provider.ToAdminDto(entity));
    }
}
