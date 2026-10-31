using FluentValidation;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Validators;

public static class StudentIdentityBusinessRules
{
    public static void ValidateIdentityTypeAllowed(IdentityType type, List<IdentityType> allowedTypes)
    {
        if (!allowedTypes.Contains(type))
            throw new ValidationException(
                $"Identity type '{type}' is not allowed for the selected nationality. Allowed types: {string.Join(", ", allowedTypes)}.");
    }

    public static void ValidateIssuingCountryRules(IdentityType type, string? countryCode, bool issuingCountryRequired)
    {
        if (issuingCountryRequired && string.IsNullOrWhiteSpace(countryCode))
            throw new ValidationException("Issuing country code is required for the selected identity type.");

        if (!issuingCountryRequired && !string.IsNullOrWhiteSpace(countryCode))
            throw new ValidationException("Issuing country code should not be provided for the selected identity type.");
    }

    public static async Task ValidateIdentityUnique(
        IStudentRepository repo,
        IdentityType type,
        string number,
        string? countryCode,
        int? excludeStudentId = null)
    {
        var isUnique = await repo.IsIdentityNumberUniqueAsync(type, number, countryCode, excludeStudentId);

        if (!isUnique)
            throw new ValidationException("This identity document is already registered to another student.");
    }
}
