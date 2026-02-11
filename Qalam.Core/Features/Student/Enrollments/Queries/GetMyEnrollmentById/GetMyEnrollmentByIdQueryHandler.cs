using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollmentById;

public class GetMyEnrollmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentByIdQuery, Response<EnrollmentDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRepository _enrollmentRepository;

    public GetMyEnrollmentByIdQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRepository enrollmentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<Response<EnrollmentDetailDto>> Handle(
        GetMyEnrollmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<EnrollmentDetailDto>("Student not found.");

        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(e => e.Course)
                .ThenInclude(c => c.SessionType)
            .Include(e => e.ApprovedByTeacher)
                .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enrollment == null || enrollment.StudentId != student.Id)
            return NotFound<EnrollmentDetailDto>("Enrollment not found.");

        var dto = new EnrollmentDetailDto
        {
            Id = enrollment.Id,
            CourseId = enrollment.CourseId,
            CourseTitle = enrollment.Course?.Title ?? "",
            CourseDescription = enrollment.Course?.Description,
            CoursePrice = enrollment.Course?.Price ?? 0,
            EnrollmentStatus = enrollment.EnrollmentStatus,
            ApprovedAt = enrollment.ApprovedAt,
            TeacherDisplayName = enrollment.ApprovedByTeacher?.User != null
                ? (enrollment.ApprovedByTeacher.User.FirstName + " " + enrollment.ApprovedByTeacher.User.LastName).Trim()
                : null,
            TeachingModeId = enrollment.Course?.TeachingModeId ?? 0,
            TeachingModeNameEn = enrollment.Course?.TeachingMode?.NameEn,
            SessionTypeId = enrollment.Course?.SessionTypeId ?? 0,
            SessionTypeNameEn = enrollment.Course?.SessionType?.NameEn
        };

        return Success(entity: dto);
    }
}
