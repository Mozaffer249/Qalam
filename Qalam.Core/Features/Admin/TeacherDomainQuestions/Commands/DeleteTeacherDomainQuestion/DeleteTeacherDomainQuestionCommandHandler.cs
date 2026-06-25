using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.DeleteTeacherDomainQuestion;

public class DeleteTeacherDomainQuestionCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTeacherDomainQuestionCommand, Response<string>>
{
    private readonly ITeacherDomainQuestionRepository _repository;

    public DeleteTeacherDomainQuestionCommandHandler(
        ITeacherDomainQuestionRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<string>> Handle(
        DeleteTeacherDomainQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<string>("Domain question not found");

        if (entity.IsSystem)
            return BadRequest<string>("System questions cannot be deleted. Deactivate instead.");

        if (await _repository.HasSubmissionsAsync(request.Id, cancellationToken))
            return BadRequest<string>("Cannot delete: teachers have already submitted this question.");

        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();

        return Success<string>("Domain question deleted");
    }
}
