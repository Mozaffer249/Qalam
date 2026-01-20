using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.EncryptColumn.Attribute;
using Microsoft.AspNetCore.Identity;
using Qalam.Data.Entity.Student;

namespace Qalam.Data.Entity.Identity
{
    public class User : IdentityUser<int>
    {
        public User()
        {
            UserRefreshTokens = new HashSet<UserRefreshToken>();
            LoginSessions = new HashSet<LoginSession>();
            TrustedDevices = new HashSet<TrustedDevice>();
            AuditLogs = new HashSet<AuditLog>();
            SecurityEvents = new HashSet<SecurityEvent>();
            PasswordHistories = new HashSet<PasswordHistory>();
            TwoFactorRecoveryCodes = new HashSet<TwoFactorRecoveryCode>();
        }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? Nationality { get; set; }

        [EncryptColumn]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;

        // Password Security
        public DateTime? PasswordChangedAt { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public int PasswordExpiryDays { get; set; } = 90; // 0 = never expires

        // OAuth/Social Login
        public string? GoogleId { get; set; }
        public string? FacebookId { get; set; }
        public string? MicrosoftId { get; set; }
        public string? AppleId { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [InverseProperty(nameof(UserRefreshToken.User))]
        public ICollection<UserRefreshToken> UserRefreshTokens { get; set; }

        public ICollection<LoginSession> LoginSessions { get; set; }
        public ICollection<TrustedDevice> TrustedDevices { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
        public ICollection<SecurityEvent> SecurityEvents { get; set; }
        public ICollection<PasswordHistory> PasswordHistories { get; set; }
        public ICollection<TwoFactorRecoveryCode> TwoFactorRecoveryCodes { get; set; }

        // Profile Navigation Properties
        public Student.Student? StudentProfile { get; set; }
        public Student.Guardian? GuardianProfile { get; set; }
    }
}

