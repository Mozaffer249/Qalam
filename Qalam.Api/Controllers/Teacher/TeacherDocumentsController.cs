using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Bases;
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
    private readonly ITeacherManagementService _teacherManagementService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IWebHostEnvironment _environment;

    public TeacherDocumentsController(
        ITeacherManagementService teacherManagementService,
        ITeacherRepository teacherRepository,
        IWebHostEnvironment environment)
    {
        _teacherManagementService = teacherManagementService;
        _teacherRepository = teacherRepository;
        _environment = environment;
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
        {
            return NewResult(new Response<List<TeacherDocumentReviewDto>?>("Teacher profile not found") { StatusCode = HttpStatusCode.NotFound });
        }

        var documents = await _teacherManagementService.GetTeacherDocumentsStatusAsync(teacher.Id);
        return NewResult(new Response<List<TeacherDocumentReviewDto>>(documents) { StatusCode = HttpStatusCode.OK });
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
        var userId = GetUserId();
        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        
        if (teacher == null)
        {
            return NewResult(new Response<bool>("Teacher profile not found") { StatusCode = HttpStatusCode.NotFound });
        }

        // Validate file
        if (file == null || file.Length == 0)
        {
            return NewResult(new Response<bool>("No file provided") { StatusCode = HttpStatusCode.BadRequest });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return NewResult(new Response<bool>("Invalid file type. Allowed: jpg, jpeg, png, pdf") { StatusCode = HttpStatusCode.BadRequest });
        }

        // Save file
        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "teachers", teacher.Id.ToString(), "reupload");
        Directory.CreateDirectory(uploadsFolder);
        
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/teachers/{teacher.Id}/reupload/{uniqueFileName}";

        var result = await _teacherManagementService.ReuploadDocumentAsync(teacher.Id, documentId, relativePath);
        
        if (!result)
        {
            // Delete uploaded file if operation failed
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            return NewResult(new Response<bool>("Failed to re-upload document. Document may not be in rejected status.") { StatusCode = HttpStatusCode.BadRequest });
        }
        
        return NewResult(new Response<bool>(true, "Document re-uploaded successfully and is pending review") { StatusCode = HttpStatusCode.OK });
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}
