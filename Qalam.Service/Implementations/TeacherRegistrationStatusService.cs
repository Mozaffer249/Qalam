using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationStatusService : ITeacherRegistrationStatusService
{
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly ITeacherRegistrationSubmissionRepository _submissionRepository;
    private readonly ITeacherDocumentRepository _documentRepository;

    public TeacherRegistrationStatusService(
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        ITeacherDocumentRepository documentRepository)
    {
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
    }

    public async Task<TeacherRegistrationStatusResponseDto> GetStatusForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var requirements = await GetChecklistForTeacherAsync(teacherId, cancellationToken);
        var legacy = await _documentRepository.GetDocumentsStatusAsync(teacherId);

        return new TeacherRegistrationStatusResponseDto
        {
            Requirements = requirements,
            LegacyDocuments = legacy
        };
    }

    public async Task<List<TeacherRegistrationSubmissionStatusDto>> GetChecklistForTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var active = await _requirementRepository.GetActiveOrderedAsync(cancellationToken);
        var submissions = await _submissionRepository.GetByTeacherIdWithRequirementsAsync(teacherId, cancellationToken);

        var result = new List<TeacherRegistrationSubmissionStatusDto>();

        foreach (var req in active)
        {
            var related = submissions.Where(s => s.RequirementId == req.Id).ToList();
            var isMultiFile = req.RequirementType == RegistrationRequirementType.File && req.MaxCount > 1;

            if (isMultiFile)
            {
                var count = related.Count;
                var worst = related.OrderByDescending(s => (int)s.VerificationStatus).FirstOrDefault();
                result.Add(new TeacherRegistrationSubmissionStatusDto
                {
                    Code = req.Code,
                    NameAr = req.NameAr,
                    NameEn = req.NameEn,
                    RequirementType = req.RequirementType.ToString(),
                    IsRequired = req.IsRequired,
                    IsSubmitted = count >= req.MinCount,
                    VerificationStatus = worst?.VerificationStatus,
                    RejectionReason = worst?.RejectionReason
                });
            }
            else
            {
                var sub = related.FirstOrDefault();
                result.Add(new TeacherRegistrationSubmissionStatusDto
                {
                    Code = req.Code,
                    NameAr = req.NameAr,
                    NameEn = req.NameEn,
                    RequirementType = req.RequirementType.ToString(),
                    IsRequired = req.IsRequired,
                    IsSubmitted = sub != null,
                    VerificationStatus = sub?.VerificationStatus,
                    RejectionReason = sub?.RejectionReason,
                    TeacherDocumentId = sub?.TeacherDocumentId,
                    TextValue = sub?.TextValue,
                    BoolValue = sub?.BoolValue
                });
            }
        }

        return result;
    }
}
