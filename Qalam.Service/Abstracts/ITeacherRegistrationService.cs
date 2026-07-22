using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationService
{
    Task<PhoneVerificationDto> CreateBasicAccountAsync(
        string fullPhoneNumber,
        string? email = null,
        DateTime? termsAcceptedAt = null);

    Task<TeacherAccountDto> CompleteAccountAsync(
        int userId,
        string firstName,
        string lastName,
        string? email,
        string password);

    Task CompleteDocumentUploadAsync(
        int teacherId,
        TeacherLocation location);

    /// <summary>
    /// Ensures the identity user has the Teacher role so teacher JWT endpoints authorize correctly.
    /// Safe to call for users who already have the role (e.g. returning registrants with Student roles).
    /// </summary>
    Task EnsureTeacherRoleForUserAsync(int userId);

    /// <summary>
    /// Determines the next registration step for a user
    /// </summary>
    Task<RegistrationStepDto> GetNextRegistrationStepAsync(int userId);
}
