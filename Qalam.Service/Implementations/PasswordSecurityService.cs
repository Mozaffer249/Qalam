using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Service.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class PasswordSecurityService : IPasswordSecurityService
    {
        private readonly IGenericRepositoryAsync<PasswordHistory> _passwordHistoryRepository;
        private readonly UserManager<User> _userManager;
        private readonly PasswordPolicySettings _policySettings;

        // Common weak passwords list (sample - should be expanded)
        private static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "123456789", "12345678", "12345", "1234567",
            "password123", "admin", "letmein", "welcome", "monkey", "qwerty",
            "abc123", "111111", "123123", "password1", "admin123"
        };

        public PasswordSecurityService(
            IGenericRepositoryAsync<PasswordHistory> passwordHistoryRepository,
            UserManager<User> userManager,
            IOptions<PasswordPolicySettings> policySettings)
        {
            _passwordHistoryRepository = passwordHistoryRepository;
            _userManager = userManager;
            _policySettings = policySettings.Value;
        }

        public async Task<bool> IsPasswordInHistoryAsync(int userId, string newPassword, int historyCount = 5)
        {
            var passwordHistories = await _passwordHistoryRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ChangedAt)
                .Take(historyCount)
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var passwordHasher = new PasswordHasher<User>();

            foreach (var history in passwordHistories)
            {
                var result = passwordHasher.VerifyHashedPassword(user, history.PasswordHash, newPassword);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true; // Password found in history
                }
            }

            return false;
        }

        public async Task AddToPasswordHistoryAsync(int userId, string passwordHash)
        {
            var passwordHistory = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                ChangedAt = DateTime.UtcNow
            };

            await _passwordHistoryRepository.AddAsync(passwordHistory);

            // Clean up old history (keep only last N passwords)
            var allHistory = await _passwordHistoryRepository.GetTableNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();

            var toDelete = allHistory.Skip(_policySettings.PreventPasswordReuse).ToList();
            foreach (var old in toDelete)
            {
                await _passwordHistoryRepository.DeleteAsync(old);
            }
        }

        public Task<PasswordStrength> CheckPasswordStrengthAsync(string password)
        {
            var strength = new PasswordStrength { Score = 0 };

            if (string.IsNullOrEmpty(password))
            {
                strength.Feedback.Add("Password is required");
                return Task.FromResult(strength);
            }

            // Length check
            if (password.Length >= 12)
                strength.Score++;
            else if (password.Length >= 8)
                strength.Feedback.Add("Password is acceptable length but could be longer");
            else
                strength.Feedback.Add("Password is too short (minimum 8 characters)");

            // Complexity checks
            if (password.Any(char.IsUpper))
                strength.Score++;
            else
                strength.Feedback.Add("Add uppercase letters for better security");

            if (password.Any(char.IsLower))
                strength.Score++;
            else
                strength.Feedback.Add("Add lowercase letters for better security");

            if (password.Any(char.IsDigit))
                strength.Score++;
            else
                strength.Feedback.Add("Add numbers for better security");

            if (password.Any(ch => !char.IsLetterOrDigit(ch)))
                strength.Score++;
            else
                strength.Feedback.Add("Add special characters for better security");

            // Cap at 4
            if (strength.Score > 4)
                strength.Score = 4;

            // Common password check
            if (CommonPasswords.Contains(password))
            {
                strength.Score = 0;
                strength.Feedback.Add("This is a commonly used password. Please choose a more unique password.");
            }

            return Task.FromResult(strength);
        }

        public Task<bool> IsCommonPasswordAsync(string password)
        {
            return Task.FromResult(CommonPasswords.Contains(password));
        }

        public async Task<bool> IsPasswordExpiredAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            // If password expiry is disabled (0), password never expires
            if (user.PasswordExpiryDays == 0)
                return false;

            // If PasswordChangedAt is null, assume it was set on account creation
            if (!user.PasswordChangedAt.HasValue)
                return false;

            var expiryDate = user.PasswordChangedAt.Value.AddDays(user.PasswordExpiryDays);
            return DateTime.UtcNow > expiryDate;
        }
    }
}
