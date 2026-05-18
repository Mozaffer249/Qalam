using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherRegistrationService
{
    Task<PhoneVerificationDto> CreateBasicAccountAsync(string fullPhoneNumber, string? email = null);

    Task<TeacherAccountDto> CompleteAccountAsync(
        int userId,
        string firstName,
        string lastName,
        string? email,
        string password);

    Task CompleteDocumentUploadAsync(
        int teacherId,
        bool isInSaudiArabia);

    /// <summary>
    /// Determines the next registration step for a user
    /// </summary>
    Task<RegistrationStepDto> GetNextRegistrationStepAsync(int userId);
}
