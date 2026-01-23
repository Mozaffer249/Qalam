using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Admin;

/// <summary>
/// Admin endpoints for managing teacher activation and document verification
/// </summary>
[ApiController]
[Route("Api/V1/Admin/[controller]")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class TeacherManagementController : AppControllerBase
{
    private readonly ITeacherManagementService _teacherManagementService;

    public TeacherManagementController(ITeacherManagementService teacherManagementService)
    {
        _teacherManagementService = teacherManagementService;
    }

    /// <summary>
    /// Get list of teachers pending verification or with rejected documents
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    [HttpGet("Pending")]
    public async Task<IActionResult> GetPendingTeachers(int pageNumber = 1, int pageSize = 10)
    {
        var result = await _teacherManagementService.GetPendingTeachersAsync(pageNumber, pageSize);
        return NewResult(new Response<object>(result) { StatusCode = HttpStatusCode.OK });
    }

    /// <summary>
    /// Get teacher details with all documents for review
    /// </summary>
    /// <param name="teacherId">Teacher ID</param>
    [HttpGet("{teacherId:int}")]
    public async Task<IActionResult> GetTeacherDetails(int teacherId)
    {
        var result = await _teacherManagementService.GetTeacherDetailsAsync(teacherId);
        if (result == null)
        {
            return NewResult(new Response<TeacherDetailsDto?>("Teacher not found") { StatusCode = HttpStatusCode.NotFound });
        }
        return NewResult(new Response<TeacherDetailsDto?>(result) { StatusCode = HttpStatusCode.OK });
    }

    /// <summary>
    /// Approve a specific document for a teacher
    /// </summary>
    /// <param name="teacherId">Teacher ID</param>
    /// <param name="documentId">Document ID</param>
    [HttpPost("{teacherId:int}/Documents/{documentId:int}/Approve")]
    public async Task<IActionResult> ApproveDocument(int teacherId, int documentId)
    {
        var adminId = GetAdminId();
        var result = await _teacherManagementService.ApproveDocumentAsync(teacherId, documentId, adminId);
        
        if (!result)
        {
            return NewResult(new Response<bool>("Failed to approve document") { StatusCode = HttpStatusCode.BadRequest });
        }
        
        return NewResult(new Response<bool>(true, "Document approved successfully") { StatusCode = HttpStatusCode.OK });
    }

    /// <summary>
    /// Reject a specific document for a teacher with reason
    /// </summary>
    /// <param name="teacherId">Teacher ID</param>
    /// <param name="documentId">Document ID</param>
    /// <param name="request">Rejection reason</param>
    [HttpPost("{teacherId:int}/Documents/{documentId:int}/Reject")]
    public async Task<IActionResult> RejectDocument(int teacherId, int documentId, [FromBody] RejectDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return NewResult(new Response<bool>("Rejection reason is required") { StatusCode = HttpStatusCode.BadRequest });
        }

        var adminId = GetAdminId();
        var result = await _teacherManagementService.RejectDocumentAsync(teacherId, documentId, adminId, request.Reason);
        
        if (!result)
        {
            return NewResult(new Response<bool>("Failed to reject document") { StatusCode = HttpStatusCode.BadRequest });
        }
        
        return NewResult(new Response<bool>(true, "Document rejected successfully. Teacher will be notified to re-upload.") { StatusCode = HttpStatusCode.OK });
    }

    /// <summary>
    /// Block a teacher account
    /// </summary>
    /// <param name="teacherId">Teacher ID</param>
    /// <param name="request">Block reason (optional)</param>
    [HttpPost("{teacherId:int}/Block")]
    public async Task<IActionResult> BlockTeacher(int teacherId, [FromBody] RejectDocumentRequest? request)
    {
        var adminId = GetAdminId();
        var result = await _teacherManagementService.BlockTeacherAsync(teacherId, adminId, request?.Reason);
        
        if (!result)
        {
            return NewResult(new Response<bool>("Failed to block teacher") { StatusCode = HttpStatusCode.BadRequest });
        }
        
        return NewResult(new Response<bool>(true, "Teacher account blocked successfully") { StatusCode = HttpStatusCode.OK });
    }

    private int GetAdminId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
