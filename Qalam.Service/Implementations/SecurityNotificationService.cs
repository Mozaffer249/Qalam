using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;
using Qalam.Service.Models;
using System;
using System.Threading.Tasks;

namespace Qalam.Service.Implementations
{
    public class SecurityNotificationService : ISecurityNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public SecurityNotificationService(IEmailService emailService, IAuditService auditService)
        {
            _emailService = emailService;
            _auditService = auditService;
        }

        public async Task NotifyPasswordChangedAsync(User user, string ipAddress)
        {
            var subject = "Password Changed - Qalam";
            var body = GeneratePasswordChangedEmail(user.FirstName, ipAddress, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
            
            // Mark security event as notified if exists
            await MarkSecurityEventNotified(user.Id, SecurityEventType.PasswordChanged);
        }

        public async Task NotifyEmailChangedAsync(string oldEmail, string newEmail, string userName, string ipAddress)
        {
            var subject = "Email Address Changed - Qalam";
            var body = GenerateEmailChangedEmail(userName, oldEmail, newEmail, ipAddress, DateTime.UtcNow);
            
            // Notify the OLD email address about the change
            await _emailService.SendEmailAsync(oldEmail, subject, body, EmailSendingStrategy.Queued);
        }

        public async Task NotifyNewDeviceLoginAsync(User user, string deviceInfo, string ipAddress)
        {
            var subject = "New Device Login Detected - Qalam";
            var body = GenerateNewDeviceLoginEmail(user.FirstName, deviceInfo, ipAddress, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
            
            await MarkSecurityEventNotified(user.Id, SecurityEventType.LoginFromNewDevice);
        }

        public async Task NotifyTwoFactorEnabledAsync(User user)
        {
            var subject = "Two-Factor Authentication Enabled - Qalam";
            var body = GenerateTwoFactorEnabledEmail(user.FirstName, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
            
            await MarkSecurityEventNotified(user.Id, SecurityEventType.TwoFactorEnabled);
        }

        public async Task NotifyTwoFactorDisabledAsync(User user)
        {
            var subject = "Two-Factor Authentication Disabled - Qalam";
            var body = GenerateTwoFactorDisabledEmail(user.FirstName, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
            
            await MarkSecurityEventNotified(user.Id, SecurityEventType.TwoFactorDisabled);
        }

        public async Task NotifySessionTerminatedAsync(User user, string deviceInfo)
        {
            var subject = "Session Terminated - Qalam";
            var body = GenerateSessionTerminatedEmail(user.FirstName, deviceInfo, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
        }

        public async Task NotifySuspiciousActivityAsync(User user, string activity, string ipAddress)
        {
            var subject = "Suspicious Activity Detected - Qalam";
            var body = GenerateSuspiciousActivityEmail(user.FirstName, activity, ipAddress, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Direct);
            
            await MarkSecurityEventNotified(user.Id, SecurityEventType.SuspiciousActivity);
        }

        public async Task NotifyAccountDeletedAsync(User user)
        {
            var subject = "Account Deleted - Qalam";
            var body = GenerateAccountDeletedEmail(user.FirstName, DateTime.UtcNow);
            
            await _emailService.SendEmailAsync(user.Email!, subject, body, EmailSendingStrategy.Queued);
        }

        private async Task MarkSecurityEventNotified(int userId, SecurityEventType eventType)
        {
            // This would require a new method in IAuditService to update WasNotified flag
            // For now, we'll skip this implementation detail
            await Task.CompletedTask;
        }

        #region Email Templates

        private string GeneratePasswordChangedEmail(string userName, string ipAddress, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Changed</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>Your password was successfully changed.</p>
            <div class='info'>
                <strong>Details:</strong><br/>
                IP Address: {ipAddress}<br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <div class='alert'>
                <strong>⚠️ Didn't make this change?</strong><br/>
                If you didn't change your password, please contact our support team immediately and secure your account.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateEmailChangedEmail(string userName, string oldEmail, string newEmail, string ipAddress, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Email Address Changed</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>Your email address has been changed on your Qalam account.</p>
            <div class='info'>
                <strong>Details:</strong><br/>
                Old Email: {oldEmail}<br/>
                New Email: {newEmail}<br/>
                IP Address: {ipAddress}<br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <div class='alert'>
                <strong>⚠️ Didn't make this change?</strong><br/>
                If you didn't change your email address, please contact our support team immediately.
            </div>
            <p><em>This notification was sent to your previous email address for security purposes.</em></p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateNewDeviceLoginEmail(string userName, string deviceInfo, string ipAddress, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>New Device Login</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>We detected a login to your Qalam account from a new device.</p>
            <div class='info'>
                <strong>Login Details:</strong><br/>
                Device: {deviceInfo}<br/>
                IP Address: {ipAddress}<br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <div class='alert'>
                <strong>⚠️ Was this you?</strong><br/>
                If you recognize this login, you can ignore this message. If you don't recognize this activity, please secure your account immediately by changing your password.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateTwoFactorEnabledEmail(string userName, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .success {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Two-Factor Authentication Enabled</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <div class='success'>
                <strong>✓ Great news!</strong><br/>
                Two-factor authentication has been successfully enabled on your account.
            </div>
            <p>Your account is now more secure. You'll need to provide a verification code from your authenticator app each time you log in.</p>
            <div class='info'>
                <strong>Important:</strong><br/>
                • Keep your recovery codes safe<br/>
                • Don't share your authenticator app with anyone<br/>
                • Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateTwoFactorDisabledEmail(string userName, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Two-Factor Authentication Disabled</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <div class='warning'>
                <strong>⚠️ Security Notice</strong><br/>
                Two-factor authentication has been disabled on your account.
            </div>
            <p>Your account is now less secure. We strongly recommend re-enabling two-factor authentication to protect your account.</p>
            <div class='info'>
                <strong>Details:</strong><br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <p>If you didn't make this change, please contact our support team immediately and secure your account.</p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateSessionTerminatedEmail(string userName, string deviceInfo, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Session Terminated</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>A session on your Qalam account was terminated.</p>
            <div class='info'>
                <strong>Details:</strong><br/>
                Device: {deviceInfo}<br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <p>If you didn't terminate this session, please review your account security and change your password.</p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateSuspiciousActivityEmail(string userName, string activity, string ipAddress, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .alert {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 10px; margin: 10px 0; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚨 Suspicious Activity Detected</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <div class='alert'>
                <strong>⚠️ URGENT: Security Alert</strong><br/>
                We detected suspicious activity on your Qalam account.
            </div>
            <div class='info'>
                <strong>Activity Details:</strong><br/>
                Activity: {activity}<br/>
                IP Address: {ipAddress}<br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <p><strong>Recommended Actions:</strong></p>
            <ul>
                <li>Change your password immediately</li>
                <li>Review your recent account activity</li>
                <li>Enable two-factor authentication if not already enabled</li>
                <li>Contact our support team if you need assistance</li>
            </ul>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateAccountDeletedEmail(string userName, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #6c757d; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .info {{ background-color: #e7f3ff; padding: 10px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Account Deleted</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>Your Qalam account has been successfully deleted as requested.</p>
            <div class='info'>
                <strong>Details:</strong><br/>
                Time: {timestamp:yyyy-MM-dd HH:mm:ss} UTC
            </div>
            <p>All your personal data has been removed from our system. Thank you for using Qalam.</p>
            <p>If you change your mind, you can create a new account at any time.</p>
        </div>
        <div class='footer'>
            <p>This is an automated notification from Qalam.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}
