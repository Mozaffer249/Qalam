using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Commands.ReuploadTeacherDocument;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher endpoints for registration submission status, document re-upload, and verification tracking.
/// </summary>
[ApiController]
[Route("Api/V1/Teacher/[controller]")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherDocumentsController : AppControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITeacherRegistrationStatusService _registrationStatusService;
    private readonly ITeacherRepository _teacherRepository;

    public TeacherDocumentsController(
        IMediator mediator,
        ITeacherRegistrationStatusService registrationStatusService,
        ITeacherRepository teacherRepository)
    {
        _mediator = mediator;
        _registrationStatusService = registrationStatusService;
        _teacherRepository = teacherRepository;
    }

    /// <summary>
    /// Get registration requirement checklist and legacy document list.
    /// </summary>
    /// <returns>
    /// `requirements` — per active catalog item (submitted flag, verification status, rejection reason);
    /// `legacyDocuments` — raw `TeacherDocument` rows for backward compatibility.
    /// </returns>
    /// <remarks>
    /// Requires **Teacher** JWT. Use after submit to track admin review progress.
    /// Teacher becomes **Active** when all **active required** submissions are approved.
    ///
    /// See `docs/Teacher-Registration-Requirements.md`.
    /// </remarks>
    [HttpGet("Status")]
    [Tags("Teacher · Documents")]
    [ProducesResponseType(typeof(TeacherRegistrationStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentsStatus()
    {
        var userId = GetUserId();
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);

        if (teacher == null)
            return NewResult(new Core.Bases.Response<TeacherRegistrationStatusResponseDto?>("Teacher profile not found")
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var status = await _registrationStatusService.GetStatusForTeacherAsync(teacher.Id);
        return NewResult(new Core.Bases.Response<TeacherRegistrationStatusResponseDto>(status)
        {
            StatusCode = HttpStatusCode.OK
        });
    }

    /// <summary>
    /// Re-upload a rejected document.
    /// </summary>
    /// <param name="documentId">Document ID from status or legacy document list</param>
    /// <param name="file">Replacement file (same validation as original upload)</param>
    /// <remarks>Only documents with `VerificationStatus` **Rejected** can be re-uploaded.</remarks>
    [HttpPut("{documentId:int}/Reupload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReuploadDocument(int documentId, IFormFile file)
    {
        var response = await _mediator.Send(new ReuploadTeacherDocumentCommand
        {
            DocumentId = documentId,
            File = file
        });
        return NewResult(response);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
