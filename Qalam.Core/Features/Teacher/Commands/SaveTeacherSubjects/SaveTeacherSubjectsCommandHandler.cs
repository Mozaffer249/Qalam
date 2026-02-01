using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;

public class SaveTeacherSubjectsCommandHandler : ResponseHandler,
    IRequestHandler<SaveTeacherSubjectsCommand, Response<TeacherSubjectsResponseDto>>
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public SaveTeacherSubjectsCommandHandler(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<Response<TeacherSubjectsResponseDto>> Handle(
        SaveTeacherSubjectsCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<TeacherSubjectsResponseDto>("Teacher not found");
        }

        // Validate subjects exist
        foreach (var subjectDto in request.Subjects)
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectDto.SubjectId);
            if (subject == null)
            {
                return BadRequest<TeacherSubjectsResponseDto>($"Subject with ID {subjectDto.SubjectId} not found");
            }

            // Validate: if CanTeachFullSubject is false, units must be provided
            if (!subjectDto.CanTeachFullSubject && !subjectDto.Units.Any())
            {
                return BadRequest<TeacherSubjectsResponseDto>(
                    $"Units are required when CanTeachFullSubject is false for subject {subject.NameAr}");
            }
        }

        // Save subjects
        var savedSubjects = await _teacherSubjectRepository.SaveTeacherSubjectsAsync(
            teacher.Id,
            request.Subjects);

        // Map to response DTO using AutoMapper
        var response = new TeacherSubjectsResponseDto
        {
            TeacherId = teacher.Id,
            Subjects = _mapper.Map<List<TeacherSubjectResponseDto>>(savedSubjects)
        };

        return Success( "Teacher subjects saved successfully", entity: response);
    }
}
