using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Queries.GetTeacherDomainQuestionById;

public class GetTeacherDomainQuestionByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherDomainQuestionByIdQuery, Response<TeacherDomainQuestionAdminDto>>
{
    private readonly ITeacherDomainQuestionRepository _repository;
    private readonly ITeacherDomainQuestionProvider _provider;

    public GetTeacherDomainQuestionByIdQueryHandler(
        ITeacherDomainQuestionRepository repository,
        ITeacherDomainQuestionProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherDomainQuestionAdminDto>> Handle(
        GetTeacherDomainQuestionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDomainAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<TeacherDomainQuestionAdminDto>("Domain question not found");

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
