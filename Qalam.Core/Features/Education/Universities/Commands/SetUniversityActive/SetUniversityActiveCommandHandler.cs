using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Universities.Commands.SetUniversityActive;

public class SetUniversityActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetUniversityActiveCommand, Response<bool>>
{
    private readonly IUniversityRepository _repo;

    public SetUniversityActiveCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IUniversityRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<bool>> Handle(SetUniversityActiveCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<bool>("University not found");

        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);
        return Success(entity: true, Message: "University status updated");
    }
}
