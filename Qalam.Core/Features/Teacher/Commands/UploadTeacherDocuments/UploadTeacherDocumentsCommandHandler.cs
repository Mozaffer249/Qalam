using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;
using Qalam.Core.Resources.Shared;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

/// <summary>
/// Legacy endpoint wrapper — delegates to <see cref="SubmitTeacherRegistrationRequirementsCommandHandler"/>.
/// </summary>
public class UploadTeacherDocumentsCommandHandler : ResponseHandler,
    IRequestHandler<UploadTeacherDocumentsCommand, Response<string>>
{
    private readonly IMediator _mediator;

    public UploadTeacherDocumentsCommandHandler(
        IMediator mediator,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _mediator = mediator;
    }

    public Task<Response<string>> Handle(UploadTeacherDocumentsCommand request, CancellationToken cancellationToken)
    {
        var submit = new SubmitTeacherRegistrationRequirementsCommand
        {
            UserId = request.UserId,
            IsInSaudiArabia = request.IsInSaudiArabia,
            IdentityType = request.IdentityType,
            DocumentNumber = request.DocumentNumber,
            IssuingCountryCode = request.IssuingCountryCode,
            IdentityDocumentFile = request.IdentityDocumentFile,
            Certificates = request.Certificates,
            CustomFilesByCode = request.CustomFilesByCode
        };

        return _mediator.Send(submit, cancellationToken);
    }
}
