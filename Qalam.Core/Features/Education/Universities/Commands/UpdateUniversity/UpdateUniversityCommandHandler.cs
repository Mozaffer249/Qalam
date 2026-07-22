using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Universities.Commands.UpdateUniversity;

public class UpdateUniversityCommandHandler : ResponseHandler,
    IRequestHandler<UpdateUniversityCommand, Response<UniversityDto>>
{
    private readonly IUniversityRepository _repo;

    public UpdateUniversityCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IUniversityRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<UniversityDto>> Handle(UpdateUniversityCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<UniversityDto>("University not found");

        existing.NameAr = request.NameAr;
        existing.NameEn = request.NameEn;
        existing.Code = request.Code;
        existing.Country = request.Country;
        existing.City = request.City;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);

        var dto = await _repo.GetUniversityDtoByIdAsync(existing.Id, cancellationToken);
        return Success(entity: dto!);
    }
}
