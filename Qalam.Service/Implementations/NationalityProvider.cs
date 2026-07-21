using Qalam.Data.DTOs.Common;
using Qalam.Data.Entity.Common;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class NationalityProvider : INationalityProvider
{
    private readonly INationalityRepository _repository;

    public NationalityProvider(INationalityRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Nationality>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllOrderedAsync(cancellationToken);

    public Task<List<Nationality>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        _repository.GetActiveOrderedAsync(cancellationToken);

    public async Task<List<NationalityPublicDto>> GetActivePublicDtosAsync(CancellationToken cancellationToken = default)
    {
        var list = await GetActiveAsync(cancellationToken);
        return list.Select(ToPublicDto).ToList();
    }

    public NationalityPublicDto ToPublicDto(Nationality entity) =>
        new()
        {
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            FlagEmoji = FlagEmojiHelper.Resolve(entity.FlagEmoji, entity.Code),
            SortOrder = entity.SortOrder
        };

    public NationalityAdminDto ToAdminDto(Nationality entity) =>
        new()
        {
            Id = entity.Id,
            Code = entity.Code,
            NameAr = entity.NameAr,
            NameEn = entity.NameEn,
            FlagEmoji = FlagEmojiHelper.Resolve(entity.FlagEmoji, entity.Code),
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
}
