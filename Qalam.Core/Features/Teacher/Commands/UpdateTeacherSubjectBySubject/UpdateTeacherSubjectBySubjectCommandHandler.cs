using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubjectBySubject;

public class UpdateTeacherSubjectBySubjectCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherSubjectBySubjectCommand, Response<TeacherSubjectResponseDto>>
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public UpdateTeacherSubjectBySubjectCommandHandler(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<Response<TeacherSubjectResponseDto>> Handle(
        UpdateTeacherSubjectBySubjectCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<TeacherSubjectResponseDto>("Teacher not found");

        var existing = await _teacherSubjectRepository.GetBySubjectIdForTeacherAsync(
            teacher.Id, request.SubjectId, cancellationToken);
        if (existing == null)
            return NotFound<TeacherSubjectResponseDto>("Teacher subject not found");

        var updated = await _teacherSubjectRepository.ReplaceUnitsAsync(
            teacher.Id,
            existing.Id,
            request.CanTeachFullSubject,
            request.Units,
            request.QuranContentTypeIds,
            request.QuranLevelIds,
            cancellationToken);

        if (updated == null)
            return NotFound<TeacherSubjectResponseDto>("Teacher subject not found");

        var dto = _mapper.Map<TeacherSubjectResponseDto>(updated);
        return Success("Teacher subject updated successfully", entity: dto);
    }
}
