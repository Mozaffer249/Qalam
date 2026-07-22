using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Colleges.Commands.SetCollegeActive;

public class SetCollegeActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetCollegeActiveCommand, Response<bool>>
{
    private readonly ICollegeRepository _repo;

    public SetCollegeActiveCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICollegeRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<bool>> Handle(SetCollegeActiveCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<bool>("College not found");

        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);
        return Success(entity: true, Message: "College status updated");
    }
}
