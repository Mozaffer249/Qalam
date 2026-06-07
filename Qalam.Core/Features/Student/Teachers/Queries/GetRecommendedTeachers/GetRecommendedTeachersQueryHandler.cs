using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using StudentEntity = Qalam.Data.Entity.Student.Student;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetRecommendedTeachers;

public class GetRecommendedTeachersQueryHandler : ResponseHandler,
    IRequestHandler<GetRecommendedTeachersQuery, Response<List<TeacherCardDto>>>
{
    private const int DefaultTake = 8;

    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ITeacherRepository _teacherRepository;

    public GetRecommendedTeachersQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<TeacherCardDto>>> Handle(
        GetRecommendedTeachersQuery request,
        CancellationToken cancellationToken)
    {
        StudentEntity student;
        if (request.StudentId <= 0)
        {
            var ownStudent = await _studentRepository.GetByUserIdAsync(request.UserId);
            if (ownStudent == null)
                return BadRequest<List<TeacherCardDto>>(
                    "Specify StudentId for which learner to recommend teachers for. Your account has no student profile to infer self.");
            student = ownStudent;
        }
        else
        {
            student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student == null)
                return NotFound<List<TeacherCardDto>>("Student not found.");
        }

        var isSelf = student.UserId == request.UserId;
        if (!isSelf)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            if (guardian == null || student.GuardianId != guardian.Id)
                return Forbidden<List<TeacherCardDto>>("You don't have permission to browse teachers for this student.");
        }

        var take = request.Take is > 0 ? request.Take.Value : DefaultTake;
        var teachers = await _teacherRepository.GetRecommendedForStudentAsync(student, take, cancellationToken);
        return Success(entity: teachers);
    }
}
