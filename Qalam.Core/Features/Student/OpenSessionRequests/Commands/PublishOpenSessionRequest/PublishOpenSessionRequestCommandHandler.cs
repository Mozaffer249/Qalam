using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.PublishOpenSessionRequest;

public class PublishOpenSessionRequestCommandHandler
    : ResponseHandler, IRequestHandler<PublishOpenSessionRequestCommand, Response<OpenSessionRequestDetailDto>>
{
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IOpenSessionRequestPublishService _publishService;
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IMapper _mapper;

    public PublishOpenSessionRequestCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        IOpenSessionRequestAccessGuard accessGuard,
        IOpenSessionRequestPublishService publishService,
        IOpenSessionRequestRepository requestRepo,
        IMapper mapper) : base(sharedLocalizer)
    {
        _accessGuard = accessGuard;
        _publishService = publishService;
        _requestRepo = requestRepo;
        _mapper = mapper;
    }

    public async Task<Response<OpenSessionRequestDetailDto>> Handle(
        PublishOpenSessionRequestCommand request,
        CancellationToken cancellationToken)
    {
        var canAct = await _accessGuard.CanActOnRequestAsync(
            request.UserId, request.Id, cancellationToken);

        if (canAct is null)
            return NotFound<OpenSessionRequestDetailDto>("الطلب غير موجود");

        if (canAct == false)
            return Unauthorized<OpenSessionRequestDetailDto>("Forbidden");

        var result = await _publishService.PublishAsync(
            request.Id, request.UserId, cancellationToken);

        if (!result.Succeeded)
        {
            return result.FailureKind switch
            {
                OpenSessionRequestPublishFailureKind.NotFound =>
                    NotFound<OpenSessionRequestDetailDto>(result.Message),
                OpenSessionRequestPublishFailureKind.Forbidden =>
                    Unauthorized<OpenSessionRequestDetailDto>(result.Message ?? "Forbidden"),
                _ => BadRequest<OpenSessionRequestDetailDto>(result.Message)
            };
        }

        var detail = await _requestRepo.GetStudentDetailAsync(result.RequestId!.Value, cancellationToken);
        if (detail is null)
            return NotFound<OpenSessionRequestDetailDto>("الطلب غير موجود");

        return Success(entity: _mapper.Map<OpenSessionRequestDetailDto>(detail));
    }
}
