using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocument;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentSection;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentVersion;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.DeleteLegalDocumentSection;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.PublishLegalDocumentVersion;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.ReorderLegalDocumentSections;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.UnpublishLegalDocumentVersion;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocument;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentSection;
using Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentVersion;
using Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersionDetail;
using Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersions;
using Qalam.Core.Features.Admin.LegalDocuments.Queries.ListLegalDocuments;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Tags("Admin · Legal Documents")]
public class LegalDocumentsController : AppControllerBase
{
    [HttpGet(Router.AdminLegalDocuments)]
    [ProducesResponseType(typeof(List<LegalDocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List() =>
        NewResult(await Mediator.Send(new ListLegalDocumentsQuery()));

    [HttpPost(Router.AdminLegalDocuments)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(LegalDocumentListItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateLegalDocumentDto data) =>
        NewResult(await Mediator.Send(new CreateLegalDocumentCommand { Data = data }));

    [HttpPut(Router.AdminLegalDocumentById)]
    [ProducesResponseType(typeof(LegalDocumentListItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateLegalDocumentDto data) =>
        NewResult(await Mediator.Send(new UpdateLegalDocumentCommand { Id = id, Data = data }));

    [HttpGet(Router.AdminLegalDocumentVersions)]
    [ProducesResponseType(typeof(List<LegalDocumentVersionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVersions([FromRoute] int id) =>
        NewResult(await Mediator.Send(new GetLegalDocumentVersionsQuery { DocumentId = id }));

    [HttpPost(Router.AdminLegalDocumentVersions)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(LegalDocumentVersionDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateVersion([FromRoute] int id, [FromBody] CreateLegalDocumentVersionDto data) =>
        NewResult(await Mediator.Send(new CreateLegalDocumentVersionCommand { DocumentId = id, Data = data }));

    [HttpGet(Router.AdminLegalDocumentVersionById)]
    [ProducesResponseType(typeof(LegalDocumentVersionDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersion([FromRoute] int versionId) =>
        NewResult(await Mediator.Send(new GetLegalDocumentVersionDetailQuery { VersionId = versionId }));

    [HttpPut(Router.AdminLegalDocumentVersionById)]
    [ProducesResponseType(typeof(LegalDocumentVersionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVersion([FromRoute] int versionId, [FromBody] UpdateLegalDocumentVersionDto data) =>
        NewResult(await Mediator.Send(new UpdateLegalDocumentVersionCommand { VersionId = versionId, Data = data }));

    [HttpPost(Router.AdminLegalDocumentVersionPublish)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(LegalDocumentVersionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish([FromRoute] int versionId, [FromBody] PublishLegalDocumentVersionDto? data) =>
        NewResult(await Mediator.Send(new PublishLegalDocumentVersionCommand
        {
            VersionId = versionId,
            Data = data ?? new PublishLegalDocumentVersionDto()
        }));

    [HttpPost(Router.AdminLegalDocumentVersionUnpublish)]
    [Authorize(Roles = Roles.SuperAdmin)]
    [ProducesResponseType(typeof(LegalDocumentVersionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unpublish([FromRoute] int versionId) =>
        NewResult(await Mediator.Send(new UnpublishLegalDocumentVersionCommand { VersionId = versionId }));

    [HttpPost(Router.AdminLegalDocumentVersionSections)]
    [ProducesResponseType(typeof(LegalDocumentSectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSection([FromRoute] int versionId, [FromBody] CreateLegalDocumentSectionDto data) =>
        NewResult(await Mediator.Send(new CreateLegalDocumentSectionCommand { VersionId = versionId, Data = data }));

    [HttpPut(Router.AdminLegalDocumentVersionSectionsReorder)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderSections([FromRoute] int versionId, [FromBody] ReorderLegalDocumentSectionsDto data) =>
        NewResult(await Mediator.Send(new ReorderLegalDocumentSectionsCommand { VersionId = versionId, Data = data }));

    [HttpPut(Router.AdminLegalDocumentSectionById)]
    [ProducesResponseType(typeof(LegalDocumentSectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSection([FromRoute] int id, [FromBody] UpdateLegalDocumentSectionDto data) =>
        NewResult(await Mediator.Send(new UpdateLegalDocumentSectionCommand { SectionId = id, Data = data }));

    [HttpDelete(Router.AdminLegalDocumentSectionById)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSection([FromRoute] int id) =>
        NewResult(await Mediator.Send(new DeleteLegalDocumentSectionCommand { SectionId = id }));
}
