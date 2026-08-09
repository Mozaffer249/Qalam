using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Legal.Commands.AcceptLegalConsents;
using Qalam.Core.Features.Legal.Queries.GetPendingConsents;
using Qalam.Core.Features.Legal.Queries.GetPublishedLegalDocument;
using Qalam.Core.Features.Legal.Queries.ListPublishedLegalDocuments;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Api.Controllers.Common;

[ApiController]
[Tags("Common · Legal")]
public class LegalController : AppControllerBase
{
    [HttpGet(Router.LegalDocuments)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PublicLegalDocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List() =>
        NewResult(await Mediator.Send(new ListPublishedLegalDocumentsQuery()));

    [HttpGet(Router.LegalDocumentByCode)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicLegalDocumentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCode([FromRoute] string code) =>
        NewResult(await Mediator.Send(new GetPublishedLegalDocumentQuery { Code = code }));

    [HttpGet(Router.LegalConsentsPending)]
    [Authorize]
    [ProducesResponseType(typeof(List<PendingConsentDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Pending() =>
        NewResult(await Mediator.Send(new GetPendingConsentsQuery()));

    [HttpPost(Router.LegalConsents)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept([FromBody] AcceptLegalConsentsDto? data) =>
        NewResult(await Mediator.Send(new AcceptLegalConsentsCommand { Data = data ?? new AcceptLegalConsentsDto() }));
}
