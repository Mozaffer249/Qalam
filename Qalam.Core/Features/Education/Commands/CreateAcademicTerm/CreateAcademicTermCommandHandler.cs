using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.CreateAcademicTerm;

public class CreateAcademicTermCommandHandler : ResponseHandler,
    IRequestHandler<CreateAcademicTermCommand, Response<AcademicTerm>>
{
    private readonly IGradeService _gradeService;

    public CreateAcademicTermCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<AcademicTerm>> Handle(
        CreateAcademicTermCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var term = new AcademicTerm
            {
                CurriculumId = request.CurriculumId,
                AcademicProgramId = request.AcademicProgramId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                OrderIndex = request.OrderIndex,
                IsMandatory = request.IsMandatory,
                IsActive = request.IsActive
            };

            var result = await _gradeService.CreateTermAsync(term);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<AcademicTerm>(ex.Message);
        }
    }
}
