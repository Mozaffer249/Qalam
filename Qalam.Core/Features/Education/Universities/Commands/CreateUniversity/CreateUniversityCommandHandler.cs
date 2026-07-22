using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Universities.Commands.CreateUniversity;

public class CreateUniversityCommandHandler : ResponseHandler,
    IRequestHandler<CreateUniversityCommand, Response<UniversityDto>>
{
    private readonly IUniversityRepository _repo;

    public CreateUniversityCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IUniversityRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<UniversityDto>> Handle(CreateUniversityCommand request, CancellationToken cancellationToken)
    {
        var entity = new University
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Code = request.Code,
            Country = request.Country,
            City = request.City,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.AddAsync(entity);
        var dto = await _repo.GetUniversityDtoByIdAsync(entity.Id, cancellationToken);
        return Created(entity: dto!);
    }
}
