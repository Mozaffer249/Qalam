using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequestById;

public class GetMyEnrollmentRequestByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestByIdQuery, Response<EnrollmentRequestDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;

    public GetMyEnrollmentRequestByIdQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _requestRepository = requestRepository;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        GetMyEnrollmentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<EnrollmentRequestDetailDto>("Student not found.");

        var enrollmentRequest = await _requestRepository.GetTableNoTracking()
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (enrollmentRequest == null || enrollmentRequest.RequestedByStudentId != student.Id)
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var desc = enrollmentRequest.Course?.Description;
        var descriptionShort = desc != null && desc.Length > 150 ? desc.Substring(0, 150) + "..." : desc;

        var dto = new EnrollmentRequestDetailDto
        {
            Id = enrollmentRequest.Id,
            CourseId = enrollmentRequest.CourseId,
            CourseTitle = enrollmentRequest.Course?.Title ?? "",
            CourseDescriptionShort = descriptionShort,
            CoursePrice = enrollmentRequest.Course?.Price ?? 0,
            TeachingModeId = enrollmentRequest.TeachingModeId,
            TeachingModeNameEn = enrollmentRequest.Course?.TeachingMode?.NameEn,
            SessionTypeId = enrollmentRequest.Course?.SessionTypeId ?? 0,
            SessionTypeNameEn = enrollmentRequest.Course?.SessionType?.NameEn,
            Status = enrollmentRequest.Status,
            CreatedAt = enrollmentRequest.CreatedAt,
            Notes = enrollmentRequest.Notes
        };

        return Success(entity: dto);
    }
}
