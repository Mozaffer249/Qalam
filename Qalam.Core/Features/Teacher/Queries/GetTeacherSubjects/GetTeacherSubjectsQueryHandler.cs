using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherSubjects;

public class GetTeacherSubjectsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherSubjectsQuery, Response<TeacherSubjectsResponseDto>>
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeacherSubjectsQueryHandler(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<Response<TeacherSubjectsResponseDto>> Handle(
        GetTeacherSubjectsQuery request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<TeacherSubjectsResponseDto>("Teacher not found");
        }

        // Get subjects with units
        var subjects = await _teacherSubjectRepository.GetTeacherSubjectsWithUnitsAsync(teacher.Id);

        // Map to response DTO using AutoMapper
        var response = new TeacherSubjectsResponseDto
        {
            TeacherId = teacher.Id,
            Subjects = _mapper.Map<List<TeacherSubjectResponseDto>>(subjects)
        };

        return Success(entity: response);
    }
}
