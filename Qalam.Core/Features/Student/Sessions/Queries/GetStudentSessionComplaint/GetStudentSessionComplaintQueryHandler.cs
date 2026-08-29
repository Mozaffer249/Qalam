using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Queries.GetStudentSessionComplaint;

public class GetStudentSessionComplaintQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentSessionComplaintQuery, Response<SessionComplaintDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ISessionComplaintService _sessionComplaints;

    public GetStudentSessionComplaintQueryHandler(
        IStudentRepository studentRepository,
        ISessionComplaintService sessionComplaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _sessionComplaints = sessionComplaints;
    }

    public async Task<Response<SessionComplaintDetailDto>> Handle(
        GetStudentSessionComplaintQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<SessionComplaintDetailDto>("Student profile not found.");

        var detail = await _sessionComplaints.GetComplaintAsync(
            request.ComplaintId,
            student.Id,
            cancellationToken);

        if (detail == null)
            return NotFound<SessionComplaintDetailDto>("Complaint not found.");

        return Success(entity: detail);
    }
}
