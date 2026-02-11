using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RequestCourseEnrollment;

public class RequestCourseEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<RequestCourseEnrollmentCommand, Response<EnrollmentRequestDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ITeachingModeRepository _teachingModeRepository;

    public RequestCourseEnrollmentCommandHandler(
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ITeachingModeRepository teachingModeRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _requestRepository = requestRepository;
        _teachingModeRepository = teachingModeRepository;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        RequestCourseEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<EnrollmentRequestDetailDto>("Student not found.");

        var dto = request.Data;

        var course = await _courseRepository.GetByIdWithDetailsAsync(dto.CourseId);
        if (course == null)
            return BadRequest<EnrollmentRequestDetailDto>("Course not found.");
        if (course.Status != CourseStatus.Published || !course.IsActive)
            return BadRequest<EnrollmentRequestDetailDto>("Course is not available for enrollment.");

        var teachingMode = await _teachingModeRepository.GetByIdAsync(dto.TeachingModeId);
        if (teachingMode == null)
            return BadRequest<EnrollmentRequestDetailDto>("Invalid TeachingModeId.");
        if (course.TeachingModeId != dto.TeachingModeId)
            return BadRequest<EnrollmentRequestDetailDto>("Teaching mode does not match the course.");

        var hasPending = await _requestRepository.GetTableNoTracking()
            .AnyAsync(r => r.CourseId == dto.CourseId && r.RequestedByStudentId == student.Id && r.Status == RequestStatus.Pending, cancellationToken);
        if (hasPending)
            return BadRequest<EnrollmentRequestDetailDto>("You already have a pending enrollment request for this course.");

        var enrollmentRequest = new Qalam.Data.Entity.Course.CourseEnrollmentRequest
        {
            CourseId = dto.CourseId,
            RequestedByStudentId = student.Id,
            TeachingModeId = dto.TeachingModeId,
            Status = RequestStatus.Pending,
            Notes = dto.Notes != null && dto.Notes.Length > 400 ? dto.Notes.Substring(0, 400) : dto.Notes
        };

        await _requestRepository.AddAsync(enrollmentRequest);
        await _requestRepository.SaveChangesAsync();

        var descriptionShort = course.Description != null && course.Description.Length > 150
            ? course.Description.Substring(0, 150) + "..."
            : course.Description;

        var result = new EnrollmentRequestDetailDto
        {
            Id = enrollmentRequest.Id,
            CourseId = enrollmentRequest.CourseId,
            CourseTitle = course.Title,
            CourseDescriptionShort = descriptionShort,
            CoursePrice = course.Price,
            TeachingModeId = enrollmentRequest.TeachingModeId,
            TeachingModeNameEn = teachingMode.NameEn,
            SessionTypeId = course.SessionTypeId,
            SessionTypeNameEn = course.SessionType?.NameEn,
            Status = enrollmentRequest.Status,
            CreatedAt = enrollmentRequest.CreatedAt,
            Notes = enrollmentRequest.Notes
        };

        return Success(entity: result);
    }
}
