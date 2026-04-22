using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyInvitations;

public class GetMyInvitationsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyInvitationsQuery, Response<List<StudentInvitationListItemDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;

    public GetMyInvitationsQueryHandler(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _requestRepository = requestRepository;
    }

    public async Task<Response<List<StudentInvitationListItemDto>>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var studentIds = new List<int>();

        var ownStudent = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (ownStudent != null)
            studentIds.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            studentIds.AddRange(children.Select(c => c.Id));
        }

        if (studentIds.Count == 0)
            return NotFound<List<StudentInvitationListItemDto>>("No student profile found.");

        studentIds = studentIds.Distinct().ToList();

        var query = _requestRepository.GetPendingInvitationsForStudentsQueryable(studentIds);

        var totalCount = await query.CountAsync(cancellationToken);

        var invitations = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = invitations.Select(gm => new StudentInvitationListItemDto
        {
            InvitationId = gm.Id,
            EnrollmentRequestId = gm.CourseEnrollmentRequestId,
            CourseId = gm.CourseEnrollmentRequest.CourseId,
            CourseTitle = gm.CourseEnrollmentRequest.Course?.Title ?? "",
            InvitedStudentId = gm.StudentId,
            InvitedStudentName = gm.Student?.User != null
                ? (gm.Student.User.FirstName + " " + gm.Student.User.LastName).Trim()
                : null,
            RequestedByUserName = gm.CourseEnrollmentRequest.RequestedByUser != null
                ? (gm.CourseEnrollmentRequest.RequestedByUser.FirstName + " " + gm.CourseEnrollmentRequest.RequestedByUser.LastName).Trim()
                : null,
            CreatedAt = gm.CreatedAt,
            ConfirmationStatus = gm.ConfirmationStatus
        }).ToList();

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
    }
}
