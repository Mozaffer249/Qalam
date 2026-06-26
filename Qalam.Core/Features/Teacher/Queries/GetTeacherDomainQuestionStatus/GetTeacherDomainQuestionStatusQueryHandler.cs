using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherDomainQuestionStatus;

public class GetTeacherDomainQuestionStatusQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherDomainQuestionStatusQuery, Response<TeacherDomainQuestionStatusResponseDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDomainQuestionStatusService _statusService;

    public GetTeacherDomainQuestionStatusQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherDomainQuestionStatusService statusService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _statusService = statusService;
    }

    public async Task<Response<TeacherDomainQuestionStatusResponseDto>> Handle(
        GetTeacherDomainQuestionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherDomainQuestionStatusResponseDto>("Teacher not found");

        var status = await _statusService.GetDomainQuestionStatusAsync(teacher.Id, cancellationToken);
        return Success(entity: status);
    }
}
