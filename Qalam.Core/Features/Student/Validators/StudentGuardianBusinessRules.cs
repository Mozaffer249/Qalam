using FluentValidation;
using StudentEntity = Qalam.Data.Entity.Student.Student;
using GuardianEntity = Qalam.Data.Entity.Student.Guardian;

namespace Qalam.Core.Features.Student.Validators;

public class StudentGuardianBusinessRules
{
    /// <summary>
    /// The minor must have a guardian
    /// </summary>
    public static void ValidateMinorHasGuardian(StudentEntity student)
    {
        if (student.IsMinor && student.GuardianId == null)
            throw new ValidationException("The minor must have a guardian");
    }

    /// <summary>
    /// The adult does not need a guardian
    /// </summary>
    public static void ValidateAdultHasNoGuardian(StudentEntity student)
    {
        if (!student.IsMinor && student.GuardianId != null)
            throw new ValidationException("The adult does not need a guardian");
    }

    /// <summary>
    /// The minor must specify the relationship with the guardian
    /// </summary>
    public static void ValidateMinorHasRelation(StudentEntity student)
    {
        if (student.IsMinor && student.GuardianRelation == null)
            throw new ValidationException("The minor must specify the relationship with the guardian");
    }

    /// <summary>
    /// The guardian must have a UserId or complete contact information
    /// </summary>
    public static void ValidateGuardianHasContactInfo(GuardianEntity guardian)
    {
        if (guardian.UserId == null)
        {
            if (string.IsNullOrWhiteSpace(guardian.FullName))
                throw new ValidationException("The guardian must have a full name");

            if (string.IsNullOrWhiteSpace(guardian.Phone))
                throw new ValidationException("The guardian must have a phone number");
        }
    }

    /// <summary>
    /// The student cannot be a guardian for himself
    /// </summary>
    public static void ValidateStudentNotOwnGuardian(StudentEntity student, GuardianEntity guardian)
    {
        if (student.UserId == guardian.UserId && guardian.UserId != null)
            throw new ValidationException("The student cannot be a guardian for himself");
    }
}
