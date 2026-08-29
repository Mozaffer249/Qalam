using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.AssignSessionComplaint;

public class AssignSessionComplaintCommandHandler : ResponseHandler,
    IRequestHandler<AssignSessionComplaintCommand, Response<string>>
{
    private readonly ISessionComplaintService _complaints;

    public AssignSessionComplaintCommandHandler(
        ISessionComplaintService complaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _complaints = complaints;
    }

    public async Task<Response<string>> Handle(
        AssignSessionComplaintCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _complaints.AssignAsync(
                request.ComplaintId,
                request.UserId,
                request.AssignedToUserId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Complaint assigned.");
    }
}
