using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainQuestionSubmitService : ITeacherDomainQuestionSubmitService
{
    private static readonly string[] DefaultExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const int DefaultMaxSizeBytes = 10 * 1024 * 1024;

    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TeacherDomainQuestionSubmitService> _logger;

    public TeacherDomainQuestionSubmitService(
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        ILogger<TeacherDomainQuestionSubmitService> logger)
    {
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task SubmitAsync(
        Teacher teacher,
        TeacherDomainQuestionSubmissionInput input,
        List<TeacherDomainQuestion> activeQuestions,
        CancellationToken cancellationToken)
    {
        using var transaction = await _submissionRepository.BeginTransactionAsync();
        try
        {
            foreach (var req in activeQuestions)
            {
                var status = req.RequiresAdminReview
                    ? DocumentVerificationStatus.Pending
                    : DocumentVerificationStatus.Approved;

                switch (req.RequirementType)
                {
                    case RegistrationRequirementType.File:
                        if (input.CustomFilesByCode.TryGetValue(req.Code, out var files))
                            await ProcessFilesAsync(teacher.Id, req, files, status);
                        break;

                    case RegistrationRequirementType.Text:
                        if (input.TextValuesByCode.TryGetValue(req.Code, out var text) && !string.IsNullOrWhiteSpace(text))
                        {
                            await SaveSubmissionAsync(teacher.Id, req.Id, textValue: text.Trim(), status: status);
                        }
                        break;

                    case RegistrationRequirementType.Boolean:
                        if (input.BoolValuesByCode.TryGetValue(req.Code, out var boolValue) && boolValue.HasValue)
                        {
                            await SaveSubmissionAsync(teacher.Id, req.Id, boolValue: boolValue, status: status);
                        }
                        break;

                    case RegistrationRequirementType.Selection:
                        if (input.SelectionsByCode.TryGetValue(req.Code, out var picked) && picked.Count > 0)
                        {
                            var stored = string.Join(",", picked);
                            await SaveSubmissionAsync(teacher.Id, req.Id, textValue: stored, status: status);
                        }
                        break;
                }
            }

            await _submissionRepository.CommitAsync();
        }
        catch (DbUpdateException dbe)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(dbe, "Domain question submit DB error for teacherId={TeacherId}", teacher.Id);
            throw new InvalidOperationException(dbe.InnerException?.Message ?? dbe.Message, dbe);
        }
        catch (Exception ex)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(ex, "Domain question submit failed for teacherId={TeacherId}", teacher.Id);
            throw;
        }
    }

    private async Task ProcessFilesAsync(
        int teacherId,
        TeacherDomainQuestion req,
        List<IFormFile> files,
        DocumentVerificationStatus status)
    {
        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        if (extensions.Count == 0) extensions = DefaultExtensions.ToList();
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : DefaultMaxSizeBytes;
        var docType = req.MapsToDocumentType ?? TeacherDocumentType.Other;
        var docStatus = req.RequiresAdminReview
            ? DocumentVerificationStatus.Pending
            : DocumentVerificationStatus.Approved;

        foreach (var file in files)
        {
            if (!await _fileStorageService.ValidateFileAsync(file, extensions.ToArray(), limit))
                throw new InvalidOperationException($"File for '{req.Code}' is invalid or too large");

            var doc = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = docType,
                FilePath = "pending-upload",
                VerificationStatus = docStatus
            };

            await _documentRepository.AddAsync(doc);
            await _documentRepository.SaveChangesAsync();

            await _fileStorageService.QueueTeacherDocUploadAsync(file, teacherId, req.Code, doc.Id);

            await SaveSubmissionAsync(teacherId, req.Id, documentId: doc.Id, status: status);
        }
    }

    private async Task SaveSubmissionAsync(
        int teacherId,
        int questionId,
        int? documentId = null,
        string? textValue = null,
        bool? boolValue = null,
        DocumentVerificationStatus status = DocumentVerificationStatus.Pending)
    {
        await _submissionRepository.AddAsync(new TeacherDomainQuestionSubmission
        {
            TeacherId = teacherId,
            QuestionId = questionId,
            TeacherDocumentId = documentId,
            TextValue = textValue,
            BoolValue = boolValue,
            VerificationStatus = status
        });
        await _submissionRepository.SaveChangesAsync();
    }
}
