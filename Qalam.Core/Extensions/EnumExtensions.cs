using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Convert string to StudentAccountType enum
    /// </summary>
    public static StudentAccountType ToStudentAccountType(this string value)
    {
        return value?.ToLower() switch
        {
            "student" => StudentAccountType.Student,
            "parent" => StudentAccountType.Parent,
            "both" => StudentAccountType.Both,
            _ => throw new ArgumentException($"Invalid account type: {value}. Valid values: Student, Parent, Both")
        };
    }

    /// <summary>
    /// Convert string to UsageMode enum
    /// </summary>
    public static UsageMode ToUsageMode(this string value)
    {
        return value?.ToLower() switch
        {
            "studyself" => UsageMode.StudySelf,
            "addchildren" => UsageMode.AddChildren,
            "both" => UsageMode.Both,
            _ => throw new ArgumentException($"Invalid usage mode: {value}. Valid values: StudySelf, AddChildren, Both")
        };
    }
    
    /// <summary>
    /// Try convert string to StudentAccountType enum without throwing
    /// </summary>
    public static bool TryParseStudentAccountType(this string value, out StudentAccountType result)
    {
        try
        {
            result = value.ToStudentAccountType();
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
    
    /// <summary>
    /// Try convert string to UsageMode enum without throwing
    /// </summary>
    public static bool TryParseUsageMode(this string value, out UsageMode result)
    {
        try
        {
            result = value.ToUsageMode();
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}
