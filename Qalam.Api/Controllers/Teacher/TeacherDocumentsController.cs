using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Teacher.Commands.ReuploadTeacherDocument;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Teacher;

/// <summary>
/// Teacher endpoints for managing documents and viewing verification status
/// </summary>
[ApiController]
[Route("Api/V1/Teacher/[controller]")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherDocumentsController : AppControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITeacherManagementService _teacherManagementService;
    private readonly ITeacherRepository _teacherRepository;

    public TeacherDocumentsController(
        IMediator mediator,
        ITeacherManagementService teacherManagementService,
        ITeacherRepository teacherRepository)
    {
        _mediator = mediator;
        _teacherManagementService = teacherManagementService;
        _teacherRepository = teacherRepository;
    }

    /// <summary>
    /// Get current status of all uploaded documents
    /// Returns status for each document (Pending, Approved, Rejected with reason)
    /// </summary>
    [HttpGet("Status")]
    public async Task<IActionResult> GetDocumentsStatus()
    {
        var userId = GetUserId();
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);

        if (teacher == null)
            return NewResult(new Core.Bases.Response<List<TeacherDocumentReviewDto>?>("Teacher profile not found") { StatusCode = HttpStatusCode.NotFound });

        var documents = await _teacherManagementService.GetTeacherDocumentsStatusAsync(teacher.Id);
        return NewResult(new Core.Bases.Response<List<TeacherDocumentReviewDto>>(documents) { StatusCode = HttpStatusCode.OK });
    }

    /// <summary>
    /// Re-upload a rejected document
    /// Only rejected documents can be re-uploaded
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="file">New document file</param>
    [HttpPut("{documentId:int}/Reupload")]
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
