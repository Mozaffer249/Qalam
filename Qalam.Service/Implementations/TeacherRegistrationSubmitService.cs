using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationSubmitService : ITeacherRegistrationSubmitService
{
    private static readonly string[] DefaultExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const int DefaultMaxSizeBytes = 10 * 1024 * 1024;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationSubmissionRepository _submissionRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<TeacherRegistrationSubmitService> _logger;

    public TeacherRegistrationSubmitService(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        IFileStorageService fileStorageService,
        ILogger<TeacherRegistrationSubmitService> logger)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _submissionRepository = submissionRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task SubmitAsync(
        Teacher teacher,
        TeacherRegistrationSubmissionInput input,
        List<TeacherRegistrationRequirement> activeRequirements,
        CancellationToken cancellationToken)
    {
        using var transaction = await _submissionRepository.BeginTransactionAsync();
        try
        {
            // Wipe orphans from any prior partial-attempt — handler only delegates here when
            // teacher status permits a fresh submit, so existing rows are not real data.
            await _submissionRepository.DeleteAllForTeacherAsync(teacher.Id, cancellationToken);
            await _documentRepository.DeletePendingForTeacherAsync(teacher.Id, cancellationToken);

            foreach (var req in activeRequirements)
            {
                // Identity + certificate keep their structurally-complex shapes (multi-field tuple,
                // list-of-tuples). Everything else flows through code-keyed generic dispatch.
                if (req.Code == TeacherRegistrationRequirementCodes.IdentityDocument)
                {
                    await ProcessIdentityAsync(input, teacher.Id, req);
                    continue;
                }
                if (req.Code == TeacherRegistrationRequirementCodes.Certificate)
                {
                    await ProcessCertificatesAsync(input, teacher.Id, req);
                    continue;
                }

                switch (req.RequirementType)
                {
                    case RegistrationRequirementType.File:
                        if (input.CustomFilesByCode.TryGetValue(req.Code, out var customFiles))
                            await ProcessCustomFilesAsync(teacher.Id, req, customFiles);
                        break;

                    case RegistrationRequirementType.Text:
                        if (input.TextValuesByCode.TryGetValue(req.Code, out var text) && !string.IsNullOrWhiteSpace(text))
                        {
                            var trimmed = text.Trim();
                            // Bio has a dedicated column on Teacher consumed by admin views — mirror it.
                            if (req.Code == TeacherRegistrationRequirementCodes.Bio)
                                teacher.Bio = trimmed;
                            await SaveSubmissionAsync(teacher.Id, req.Id, textValue: trimmed,
                                status: DocumentVerificationStatus.Approved);
                        }
                        break;

                    case RegistrationRequirementType.Boolean:
                        if (input.BoolValuesByCode.TryGetValue(req.Code, out var boolValue) && boolValue.HasValue)
                        {
                            await SaveSubmissionAsync(teacher.Id, req.Id, boolValue: boolValue,
                                status: DocumentVerificationStatus.Approved);
                        }
                        break;

                    case RegistrationRequirementType.Selection:
                        if (input.SelectionsByCode.TryGetValue(req.Code, out var picked) && picked.Count > 0)
                        {
                            // Single-select stores the lone value; multi-select stores comma-joined values.
                            // Admin/teacher views resolve labels by re-parsing the requirement's OptionsJson.
                            var stored = string.Join(",", picked);
                            await SaveSubmissionAsync(teacher.Id, req.Id, textValue: stored,
                                status: DocumentVerificationStatus.Approved);
                        }
                        break;
                }
            }

            // Derive Teacher.Location from nationality (SA → Inside, else Outside).
            if (!string.IsNullOrWhiteSpace(input.NationalityCode))
            {
                teacher.Location = string.Equals(input.NationalityCode.Trim(), "SA", StringComparison.OrdinalIgnoreCase)
                    ? TeacherLocation.InsideSaudiArabia
                    : TeacherLocation.OutsideSaudiArabia;
            }

            await _teacherRepository.UpdateAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            await _submissionRepository.CommitAsync();
        }
        catch (DbUpdateException dbe)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(dbe,
                "SubmitRegistrationRequirements DB error for teacherId={TeacherId}", teacher.Id);
            // EF wraps the SQL reason in InnerException; surface it so the handler returns a useful 400.
            throw new InvalidOperationException(dbe.InnerException?.Message ?? dbe.Message, dbe);
        }
        catch (Exception ex)
        {
            await _submissionRepository.RollBackAsync();
            _logger.LogError(ex,
                "SubmitRegistrationRequirements failed for teacherId={TeacherId}", teacher.Id);
            throw;
        }
    }

    private async Task ProcessIdentityAsync(
        TeacherRegistrationSubmissionInput input,
        int teacherId,
        TeacherRegistrationRequirement req)
    {
        if (input.IdentityDocumentFile == null)
            return;

        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        if (extensions.Count == 0) extensions = DefaultExtensions.ToList();
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : DefaultMaxSizeBytes;

        if (!await _fileStorageService.ValidateFileAsync(input.IdentityDocumentFile, extensions.ToArray(), limit))
            throw new InvalidOperationException("Identity document file is invalid or too large");

        var identityDoc = new TeacherDocument
        {
            TeacherId = teacherId,
            DocumentType = TeacherDocumentType.IdentityDocument,
            FilePath = "pending-upload",
            DocumentNumber = input.DocumentNumber,
            IdentityType = input.IdentityType,
            IssuingCountryCode = input.IssuingCountryCode,
            VerificationStatus = DocumentVerificationStatus.Pending
        };

        await _documentRepository.AddAsync(identityDoc);
        await _documentRepository.SaveChangesAsync();

        await _fileStorageService.QueueTeacherDocUploadAsync(
            input.IdentityDocumentFile, teacherId, "identity", identityDoc.Id);

        await SaveSubmissionAsync(teacherId, req.Id, documentId: identityDoc.Id,
            status: DocumentVerificationStatus.Pending);
    }

    private async Task ProcessCertificatesAsync(
        TeacherRegistrationSubmissionInput input,
        int teacherId,
        TeacherRegistrationRequirement req)
    {
        if (input.Certificates.Count == 0)
            return;

        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        if (extensions.Count == 0) extensions = DefaultExtensions.ToList();
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : DefaultMaxSizeBytes;

        foreach (var cert in input.Certificates)
        {
            if (!await _fileStorageService.ValidateFileAsync(cert.File, extensions.ToArray(), limit))
                throw new InvalidOperationException(
                    $"Certificate file '{cert.File.FileName}' is invalid or too large");

            var certificate = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = TeacherDocumentType.Certificate,
                FilePath = "pending-upload",
                CertificateTitle = cert.Title,
                Issuer = cert.Issuer,
                IssueDate = cert.IssueDate,
                VerificationStatus = DocumentVerificationStatus.Pending
            };

            await _documentRepository.AddAsync(certificate);
            await _documentRepository.SaveChangesAsync();

            await _fileStorageService.QueueTeacherDocUploadAsync(
                cert.File, teacherId, "certificates", certificate.Id);

            await SaveSubmissionAsync(teacherId, req.Id, documentId: certificate.Id,
                status: DocumentVerificationStatus.Pending);
        }
    }

    private async Task ProcessCustomFilesAsync(
        int teacherId,
        TeacherRegistrationRequirement req,
        List<IFormFile> files)
    {
        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : DefaultMaxSizeBytes;
        var docType = req.MapsToDocumentType ?? TeacherDocumentType.Other;

        foreach (var file in files)
        {
            if (!await _fileStorageService.ValidateFileAsync(file, extensions.ToArray(), limit))
                throw new InvalidOperationException($"File for '{req.Code}' is invalid or too large");

            var doc = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = docType,
                FilePath = "pending-upload",
                VerificationStatus = DocumentVerificationStatus.Pending
            };

            await _documentRepository.AddAsync(doc);
            await _documentRepository.SaveChangesAsync();

            await _fileStorageService.QueueTeacherDocUploadAsync(file, teacherId, req.Code, doc.Id);

            await SaveSubmissionAsync(teacherId, req.Id, documentId: doc.Id,
                status: DocumentVerificationStatus.Pending);
        }
    }

    // Always-insert. Prior submissions for this teacher were wiped at the top of SubmitAsync,
    // so duplicate-key risk is gone. The filtered unique index
    // (TeacherId, RequirementId) WHERE TeacherDocumentId IS NULL still enforces one text/bool
    // answer per requirement while allowing many file-backed rows (multi-cert).
    private async Task SaveSubmissionAsync(
        int teacherId,
        int requirementId,
        int? documentId = null,
        string? textValue = null,
        bool? boolValue = null,
        DocumentVerificationStatus status = DocumentVerificationStatus.Pending)
    {
        await _submissionRepository.AddAsync(new TeacherRegistrationSubmission
        {
            TeacherId = teacherId,
            RequirementId = requirementId,
            TeacherDocumentId = documentId,
            TextValue = textValue,
            BoolValue = boolValue,
            VerificationStatus = status
        });
        await _submissionRepository.SaveChangesAsync();
    }
}
