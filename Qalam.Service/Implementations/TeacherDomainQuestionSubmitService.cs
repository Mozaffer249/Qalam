using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainQuestionSubmitService : ITeacherDomainQuestionSubmitService
{
    private static readonly string[] DefaultExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const int DefaultMaxSizeBytes = 10 * 1024 * 1024;

    private readonly ITeacherDomainQuestionSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ApplicationDBContext _db;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TeacherDomainQuestionSubmitService> _logger;

    public TeacherDomainQuestionSubmitService(
        ITeacherDomainQuestionSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository,
        ApplicationDBContext db,
        IFileStorageService fileStorageService,
        ILogger<TeacherDomainQuestionSubmitService> logger)
    {
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _db = db;
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

    public async Task ResubmitRejectedAsync(
        Teacher teacher,
        TeacherDomainQuestionSubmissionInput input,
        List<TeacherDomainQuestion> activeQuestions,
        Dictionary<int, TeacherDomainQuestionSubmission> existingByQuestionId,
        CancellationToken cancellationToken)
    {
        using var transaction = await _submissionRepository.BeginTransactionAsync();
        try
        {
            foreach (var req in activeQuestions)
            {
                if (!existingByQuestionId.TryGetValue(req.Id, out var existing)
                    || existing.VerificationStatus != DocumentVerificationStatus.Rejected)
                    continue;

                var status = req.RequiresAdminReview
                    ? DocumentVerificationStatus.Pending
                    : DocumentVerificationStatus.Approved;

                switch (req.RequirementType)
                {
                    case RegistrationRequirementType.File:
                        if (input.CustomFilesByCode.TryGetValue(req.Code, out var files) && files.Count > 0)
                            await ResubmitFileAsync(teacher.Id, req, existing, files, status);
                        break;

                    case RegistrationRequirementType.Text:
                        if (input.TextValuesByCode.TryGetValue(req.Code, out var text) && !string.IsNullOrWhiteSpace(text))
                            await UpdateSubmissionAsync(existing, textValue: text.Trim(), status: status);
                        break;

                    case RegistrationRequirementType.Boolean:
                        if (input.BoolValuesByCode.TryGetValue(req.Code, out var boolValue) && boolValue.HasValue)
                            await UpdateSubmissionAsync(existing, boolValue: boolValue, status: status);
                        break;

                    case RegistrationRequirementType.Selection:
                        if (input.SelectionsByCode.TryGetValue(req.Code, out var picked) && picked.Count > 0)
                        {
                            var stored = string.Join(",", picked);
                            await UpdateSubmissionAsync(existing, textValue: stored, status: status);
                        }
                        break;
                }
            }

            await _submissionRepository.CommitAsync();
        }
        catch (DbUpdateException dbe)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(dbe, "Domain question resubmit DB error for teacherId={TeacherId}", teacher.Id);
            throw new InvalidOperationException(dbe.InnerException?.Message ?? dbe.Message, dbe);
        }
        catch (Exception ex)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(ex, "Domain question resubmit failed for teacherId={TeacherId}", teacher.Id);
            throw;
        }
    }

    private async Task ResubmitFileAsync(
        int teacherId,
        TeacherDomainQuestion req,
        TeacherDomainQuestionSubmission existing,
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

        // Ensure Documents collection is loaded for replace.
        await _db.Entry(existing).Collection(s => s.Documents).LoadAsync();

        var existingLinks = existing.Documents.ToList();
        foreach (var link in existingLinks)
            _db.TeacherDomainQuestionSubmissionDocuments.Remove(link);
        existing.Documents.Clear();

        int? primaryDocumentId = null;
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

            existing.Documents.Add(new TeacherDomainQuestionSubmissionDocument
            {
                SubmissionId = existing.Id,
                TeacherDocumentId = doc.Id,
            });
            primaryDocumentId ??= doc.Id;
        }

        await UpdateSubmissionAsync(existing, documentId: primaryDocumentId, status: status);
    }

    private async Task UpdateSubmissionAsync(
        TeacherDomainQuestionSubmission existing,
        int? documentId = null,
        string? textValue = null,
        bool? boolValue = null,
        DocumentVerificationStatus? status = null)
    {
        if (documentId.HasValue)
            existing.TeacherDocumentId = documentId;
        if (textValue != null)
            existing.TextValue = textValue;
        if (boolValue.HasValue)
            existing.BoolValue = boolValue;
        if (status.HasValue)
            existing.VerificationStatus = status.Value;

        existing.RejectionReason = null;
        existing.ReviewedByAdminId = null;
        existing.ReviewedAt = null;

        await _submissionRepository.UpdateAsync(existing);
        await _submissionRepository.SaveChangesAsync();
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

        var documentIds = new List<int>();
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
            documentIds.Add(doc.Id);
        }

        if (documentIds.Count == 0)
            return;

        await SaveSubmissionAsync(
            teacherId,
            req.Id,
            documentId: documentIds[0],
            documentIds: documentIds,
            status: status);
    }

    private async Task SaveSubmissionAsync(
        int teacherId,
        int questionId,
        int? documentId = null,
        IReadOnlyList<int>? documentIds = null,
        string? textValue = null,
        bool? boolValue = null,
        DocumentVerificationStatus status = DocumentVerificationStatus.Pending)
    {
        if (await _submissionRepository.ExistsForTeacherAndQuestionAsync(teacherId, questionId))
        {
            throw new InvalidOperationException(
                $"A submission for question {questionId} already exists for this teacher and cannot be created again.");
        }

        var submission = new TeacherDomainQuestionSubmission
        {
            TeacherId = teacherId,
            QuestionId = questionId,
            TeacherDocumentId = documentId,
            TextValue = textValue,
            BoolValue = boolValue,
            VerificationStatus = status
        };

        if (documentIds is { Count: > 0 })
        {
            foreach (var id in documentIds.Distinct())
            {
                submission.Documents.Add(new TeacherDomainQuestionSubmissionDocument
                {
                    TeacherDocumentId = id,
                });
            }
        }
        else if (documentId.HasValue)
        {
            submission.Documents.Add(new TeacherDomainQuestionSubmissionDocument
            {
                TeacherDocumentId = documentId.Value,
            });
        }

        await _submissionRepository.AddAsync(submission);
        await _submissionRepository.SaveChangesAsync();
    }
}
