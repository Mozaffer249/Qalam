using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteWritableFilterValue;

public class DeleteWritableFilterValueCommandHandler : ResponseHandler,
    IRequestHandler<DeleteWritableFilterValueCommand, Response<bool>>
{
    private readonly IWritableFilterRepository _writableFilterRepository;

    public DeleteWritableFilterValueCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IWritableFilterRepository writableFilterRepository) : base(localizer)
    {
        _writableFilterRepository = writableFilterRepository;
    }

    public async Task<Response<bool>> Handle(
        DeleteWritableFilterValueCommand request,
        CancellationToken cancellationToken)
    {
        var value = await _writableFilterRepository.GetByIdAsync(request.Id);
        if (value == null)
            return NotFound<bool>("Writable filter value not found");

        if (!value.IsActive)
            return Deleted<bool>();

        value.IsActive = false;
        value.UpdatedAt = DateTime.UtcNow;
        await _writableFilterRepository.UpdateAsync(value);
        return Deleted<bool>();
    }
}
