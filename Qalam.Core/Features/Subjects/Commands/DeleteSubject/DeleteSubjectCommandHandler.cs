using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectCommandHandler : ResponseHandler,
    IRequestHandler<DeleteSubjectCommand, Response<bool>>
{
    private readonly ISubjectService _subjectService;

    public DeleteSubjectCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ISubjectService subjectService) : base(localizer)
    {
        _subjectService = subjectService;
    }

    public async Task<Response<bool>> Handle(
        DeleteSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _subjectService.DeleteSubjectAsync(request.Id);
            if (!result)
                return NotFound<bool>("Subject not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
