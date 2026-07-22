using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Colleges.Commands.CreateCollege;

public class CreateCollegeCommandHandler : ResponseHandler,
    IRequestHandler<CreateCollegeCommand, Response<CollegeDto>>
{
    private readonly ICollegeRepository _repo;
    private readonly IUniversityRepository _universities;

    public CreateCollegeCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICollegeRepository repo,
        IUniversityRepository universities) : base(localizer)
    {
        _repo = repo;
        _universities = universities;
    }

    public async Task<Response<CollegeDto>> Handle(CreateCollegeCommand request, CancellationToken cancellationToken)
    {
        if (!await _universities.ExistsAsync(request.UniversityId, cancellationToken))
            return BadRequest<CollegeDto>("University not found");

        var entity = new College
        {
            UniversityId = request.UniversityId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Code = request.Code,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.AddAsync(entity);
        var dto = await _repo.GetCollegeDtoByIdAsync(entity.Id, cancellationToken);
        return Created(entity: dto!);
    }
}
