using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;

public class SaveTeacherSubjectsCommandHandler : ResponseHandler,
    IRequestHandler<SaveTeacherSubjectsCommand, Response<TeacherSubjectsResponseDto>>
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISubjectService _subjectService;
    private readonly IMapper _mapper;

    public SaveTeacherSubjectsCommandHandler(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        ISubjectService subjectService,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
        _subjectService = subjectService;
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

        // Validate subjects exist (optimized - single batch query)
        var subjectIds = request.Subjects.Select(s => s.SubjectId);
        var invalidSubjectIds = await _subjectService.GetInvalidSubjectIdsAsync(subjectIds);
        
        if (invalidSubjectIds.Any())
        {
            return BadRequest<TeacherSubjectsResponseDto>(
                $"Subjects not found: {string.Join(", ", invalidSubjectIds)}");
        }

        // Validate: if CanTeachFullSubject is false, units must be provided
        var subjectsWithoutUnits = request.Subjects
            .Where(s => !s.CanTeachFullSubject && !s.Units.Any())
            .Select(s => s.SubjectId)
            .ToList();
            
        if (subjectsWithoutUnits.Any())
        {
            return BadRequest<TeacherSubjectsResponseDto>(
                $"Units are required when CanTeachFullSubject is false for subjects: {string.Join(", ", subjectsWithoutUnits)}");
        }

        // Add only new subjects (skip existing ones)
        var savedSubjects = await _teacherSubjectRepository.AddNewSubjectsAsync(
            teacher.Id,
            request.Subjects);

        // Map to response DTO using AutoMapper
        var response = new TeacherSubjectsResponseDto
        {
            TeacherId = teacher.Id,
            Subjects = _mapper.Map<List<TeacherSubjectResponseDto>>(savedSubjects)
        };

        return Success("Teacher subjects saved successfully", entity: response);
    }
}
