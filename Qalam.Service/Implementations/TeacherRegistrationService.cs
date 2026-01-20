using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherRegistrationService : ITeacherRegistrationService
{
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<TeacherRegistrationService> _logger;

    public TeacherRegistrationService(
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IAuthenticationService authService,
        ILogger<TeacherRegistrationService> logger)
    {
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _authService = authService;
        _logger = logger;
    }

    public async Task<PhoneVerificationDto> CreateBasicAccountAsync(string fullPhoneNumber)
    {
        // Create basic user account (email is optional, will be provided later)
        var user = new User
        {
            UserName = fullPhoneNumber,
            PhoneNumber = fullPhoneNumber,
            PhoneNumberConfirmed = true,
            IsActive = false,  
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user account: {Errors}", errors);
            throw new Exception($"Failed to create account: {errors}");
        }

        // Assign Teacher role
        await _userManager.AddToRoleAsync(user, Roles.Teacher);

        // Generate JWT token
        var jwtResult = await _authService.GetJWTToken(user);

        _logger.LogInformation("Basic teacher account created for user {UserId}", user.Id);

        return new PhoneVerificationDto
        {
            UserId = user.Id,
            Token = jwtResult.AccessToken,
            PhoneNumber = fullPhoneNumber
        };
    }

    public async Task<TeacherAccountDto> CompleteAccountAsync(
        int userId,
        string firstName,
        string lastName,
        string? email,
        string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new Exception("User not found");
        }

        // Update user details
        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.IsActive = true;

        // Set password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var passwordResult = await _userManager.ResetPasswordAsync(user, token, password);

        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            throw new Exception($"Failed to set password: {errors}");
        }

        await _userManager.UpdateAsync(user);

        // Create Teacher profile
        var teacher = new Teacher
        {
            UserId = user.Id,
            Status = TeacherStatus.Pending,
            IsActive = true
        };

        await _teacherRepository.AddAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        // Generate new JWT token with updated user info
        var jwtResult = await _authService.GetJWTToken(user);

        _logger.LogInformation(
            "Teacher account completed for user {UserId}, teacher {TeacherId}",
            user.Id,
            teacher.Id);

        return new TeacherAccountDto
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = user.PhoneNumber!,
            Token = jwtResult.AccessToken
        };
    }

    public async Task CompleteDocumentUploadAsync(
        int teacherId,
        bool isInSaudiArabia)
    {
        // Update teacher status to PendingVerification
        await _teacherRepository.UpdateStatusAsync(
            teacherId,
            TeacherStatus.PendingVerification);

        // Update teacher location
        var location = isInSaudiArabia
            ? TeacherLocation.InsideSaudiArabia
            : TeacherLocation.OutsideSaudiArabia;
        
        await _teacherRepository.UpdateLocationAsync(teacherId, location);
        
        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Document upload completed for teacher {TeacherId}, status set to PendingVerification",
            teacherId);
    }
}
