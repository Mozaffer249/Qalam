using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Commands.FileStudentSessionComplaint;

public class FileStudentSessionComplaintCommandHandler : ResponseHandler,
    IRequestHandler<FileStudentSessionComplaintCommand, Response<SessionComplaintDetailDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ISessionComplaintService _sessionComplaints;

    public FileStudentSessionComplaintCommandHandler(
        IStudentRepository studentRepository,
        ISessionComplaintService sessionComplaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _sessionComplaints = sessionComplaints;
    }

    public async Task<Response<SessionComplaintDetailDto>> Handle(
        FileStudentSessionComplaintCommand request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<SessionComplaintDetailDto>("Student profile not found.");

        try
        {
            var complaint = await _sessionComplaints.FileComplaintAsync(
                request.ScheduleId,
                student.Id,
                request.UserId,
                request.ReasonCode,
                request.Description,
                request.Attachments,
                cancellationToken);

            var detail = await _sessionComplaints.GetComplaintAsync(
                complaint.Id,
                student.Id,
                cancellationToken);

            return Success(entity: detail!);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<SessionComplaintDetailDto>(ex.Message);
        }
    }
}
