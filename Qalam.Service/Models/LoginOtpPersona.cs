namespace Qalam.Service.Models;

/// <summary>Which app persona triggered the login OTP (affects email copy).</summary>
public enum LoginOtpPersona
{
    Teacher = 0,
    Student = 1
}
