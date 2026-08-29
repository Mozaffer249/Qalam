using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Commands.RequestTeacherComplaintResponse;

public class RequestTeacherComplaintResponseCommandHandler : ResponseHandler,
    IRequestHandler<RequestTeacherComplaintResponseCommand, Response<string>>
{
    private readonly ISessionComplaintService _complaints;

    public RequestTeacherComplaintResponseCommandHandler(
        ISessionComplaintService complaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _complaints = complaints;
    }

    public async Task<Response<string>> Handle(
        RequestTeacherComplaintResponseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _complaints.RequestTeacherResponseAsync(
                request.ComplaintId,
                request.UserId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success(entity: "Teacher response requested.");
    }
}
