using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.ListTeacherDomainQuestions;

public class ListTeacherDomainQuestionsQueryHandler : ResponseHandler,
    IRequestHandler<ListTeacherDomainQuestionsQuery, Response<List<TeacherDomainQuestionAdminDto>>>
{
    private readonly ITeacherDomainQuestionRepository _repository;
    private readonly ITeacherDomainQuestionProvider _provider;

    public ListTeacherDomainQuestionsQueryHandler(
        ITeacherDomainQuestionRepository repository,
        ITeacherDomainQuestionProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<List<TeacherDomainQuestionAdminDto>>> Handle(
        ListTeacherDomainQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _repository.GetAllOrderedAsync(request.DomainId, cancellationToken);
        var dtos = entities.Select(_provider.ToAdminDto).ToList();
        return Success(entity: dtos);
    }
}
