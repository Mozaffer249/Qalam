using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Departments.Commands.SetDepartmentActive;

public class SetDepartmentActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetDepartmentActiveCommand, Response<bool>>
{
    private readonly IDepartmentRepository _repo;

    public SetDepartmentActiveCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IDepartmentRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<bool>> Handle(SetDepartmentActiveCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<bool>("Department not found");

        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);
        return Success(entity: true, Message: "Department status updated");
    }
}
