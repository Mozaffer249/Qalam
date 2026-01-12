using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Commands.CreateSubject;

public class CreateSubjectCommandHandler : ResponseHandler,
    IRequestHandler<CreateSubjectCommand, Response<Subject>>
{
    private readonly ISubjectService _subjectService;

    public CreateSubjectCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<Subject>> Handle(
        CreateSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = new Subject
            {
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

            var result = await _subjectService.CreateSubjectAsync(subject);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<Subject>(ex.Message);
        }
    }
}
