using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Commands.UpdateSubject;

public class UpdateSubjectCommandHandler : ResponseHandler,
    IRequestHandler<UpdateSubjectCommand, Response<Subject>>
{
    private readonly ISubjectService _subjectService;

    public UpdateSubjectCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<Subject>> Handle(
        UpdateSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = new Subject
            {
                Id = request.Id,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                DomainId = request.DomainId,
                CurriculumId = request.CurriculumId,
                LevelId = request.LevelId,
                GradeId = request.GradeId,
                TermId = request.TermId,
                IsActive = request.IsActive
            };

            var result = await _subjectService.UpdateSubjectAsync(subject);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Subject>(ex.Message);
        }
    }
}
