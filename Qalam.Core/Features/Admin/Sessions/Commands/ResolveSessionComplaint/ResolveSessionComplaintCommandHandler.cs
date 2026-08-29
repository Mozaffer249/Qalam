using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.ResolveSessionComplaint;

public class ResolveSessionComplaintCommandHandler : ResponseHandler,
    IRequestHandler<ResolveSessionComplaintCommand, Response<string>>
{
    private readonly ISessionComplaintService _complaints;

    public ResolveSessionComplaintCommandHandler(
        ISessionComplaintService complaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _complaints = complaints;
    }

    public async Task<Response<string>> Handle(
        ResolveSessionComplaintCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _complaints.ResolveAsync(
                request.ScheduleId,
                request.ComplaintId,
                request.UserId,
                request.Body.ResolutionCode,
                request.Body.ResolutionNotes,
                request.Body.RefundAmount,
                request.Body.PaymentId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Complaint resolved.");
    }
}
