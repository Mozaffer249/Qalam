using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationLevelById;

public class GetEducationLevelByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationLevelByIdQuery, Response<EducationLevel>>
{
    private readonly IGradeService _gradeService;

    public GetEducationLevelByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<EducationLevel>> Handle(
        GetEducationLevelByIdQuery request,
        CancellationToken cancellationToken)
    {
        var level = await _gradeService.GetLevelByIdAsync(request.Id);

        if (level == null)
            return NotFound<EducationLevel>("Education level not found");

        return Success(entity: level);
    }
}
