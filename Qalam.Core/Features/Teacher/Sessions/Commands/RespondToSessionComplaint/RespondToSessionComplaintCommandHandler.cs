using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Commands.RespondToSessionComplaint;

public class RespondToSessionComplaintCommandHandler : ResponseHandler,
    IRequestHandler<RespondToSessionComplaintCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISessionComplaintService _sessionComplaints;

    public RespondToSessionComplaintCommandHandler(
        ITeacherRepository teacherRepository,
        ISessionComplaintService sessionComplaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _sessionComplaints = sessionComplaints;
    }

    public async Task<Response<string>> Handle(
        RespondToSessionComplaintCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        try
        {
            await _sessionComplaints.RespondAsTeacherAsync(
                request.ComplaintId,
                teacher.Id,
                request.Response,
                cancellationToken);

            return Success(entity: "Response submitted.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
