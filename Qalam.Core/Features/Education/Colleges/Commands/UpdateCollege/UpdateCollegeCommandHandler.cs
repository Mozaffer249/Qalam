using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Colleges.Commands.UpdateCollege;

public class UpdateCollegeCommandHandler : ResponseHandler,
    IRequestHandler<UpdateCollegeCommand, Response<CollegeDto>>
{
    private readonly ICollegeRepository _repo;
    private readonly IUniversityRepository _universities;

    public UpdateCollegeCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICollegeRepository repo,
        IUniversityRepository universities) : base(localizer)
    {
        _repo = repo;
        _universities = universities;
    }

    public async Task<Response<CollegeDto>> Handle(UpdateCollegeCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<CollegeDto>("College not found");

        if (!await _universities.ExistsAsync(request.UniversityId, cancellationToken))
            return BadRequest<CollegeDto>("University not found");

        existing.UniversityId = request.UniversityId;
        existing.NameAr = request.NameAr;
        existing.NameEn = request.NameEn;
        existing.Code = request.Code;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);

        var dto = await _repo.GetCollegeDtoByIdAsync(existing.Id, cancellationToken);
        return Success(entity: dto!);
    }
}
