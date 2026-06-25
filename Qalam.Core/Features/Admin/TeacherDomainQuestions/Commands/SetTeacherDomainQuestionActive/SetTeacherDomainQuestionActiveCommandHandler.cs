using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.SetTeacherDomainQuestionActive;

public class SetTeacherDomainQuestionActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherDomainQuestionActiveCommand, Response<TeacherDomainQuestionAdminDto>>
{
    private readonly ITeacherDomainQuestionRepository _repository;
    private readonly ITeacherDomainQuestionProvider _provider;

    public SetTeacherDomainQuestionActiveCommandHandler(
        ITeacherDomainQuestionRepository repository,
        ITeacherDomainQuestionProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherDomainQuestionAdminDto>> Handle(
        SetTeacherDomainQuestionActiveCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDomainAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<TeacherDomainQuestionAdminDto>("Domain question not found");

        entity.IsActive = request.Data.IsActive;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
