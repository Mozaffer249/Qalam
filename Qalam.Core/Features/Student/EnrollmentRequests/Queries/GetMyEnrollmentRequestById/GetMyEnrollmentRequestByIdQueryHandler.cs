using AutoMapper;
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
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestByIdQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IGuardianRepository guardianRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _requestRepository = requestRepository;
        _guardianRepository = guardianRepository;
        _mapper = mapper;
    }

    public async Task<Response<EnrollmentRequestDetailDto>> Handle(
        GetMyEnrollmentRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Collect authorized student IDs (own profile + guardian's children)
        var authorizedStudentIds = new List<int>();

        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student != null)
            authorizedStudentIds.Add(student.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            authorizedStudentIds.AddRange(children.Select(c => c.Id));
        }

        if (authorizedStudentIds.Count == 0)
            return NotFound<EnrollmentRequestDetailDto>("No student profile found.");

        var enrollmentRequest = await _requestRepository.GetTableNoTracking()
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .Include(r => r.SelectedAvailabilities)
            .Include(r => r.GroupMembers)
            .Include(r => r.ProposedSessions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (enrollmentRequest == null || !authorizedStudentIds.Contains(enrollmentRequest.RequestedByStudentId))
            return NotFound<EnrollmentRequestDetailDto>("Enrollment request not found.");

        var dto = _mapper.Map<EnrollmentRequestDetailDto>(enrollmentRequest);

        return Success(entity: dto);
    }
}
