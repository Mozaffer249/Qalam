using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.context;
using Qalam.Service.Models;

namespace Qalam.Core.Features.Authentication.Commands.SendResetPasswordCode
{
    public class SendResetPasswordCodeCommandHandler : ResponseHandler, IRequestHandler<SendResetPasswordCodeCommand, Response<string>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendResetPasswordCodeCommandHandler> _logger;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

        private const string MessagingApiBaseUrlKey = "MessagingApi:BaseUrl";
        private const string MessagingApiEmailEndpoint = "/api/messaging/email";

        public SendResetPasswordCodeCommandHandler(
            IStringLocalizer<AuthenticationResources> authLocalizer,
            UserManager<User> userManager,
            ApplicationDBContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SendResetPasswordCodeCommandHandler> logger) : base(authLocalizer)
        {
            _userManager = userManager;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _authLocalizer = authLocalizer;
        }

        public async Task<Response<string>> Handle(SendResetPasswordCodeCommand request, CancellationToken cancellationToken)
        {
            // Check if user exists
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.EmailIsNotExist]);
            }

            if (!user.IsActive)
            {
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.UserIsNotActive]);
            }

            // Generate 6-digit OTP
            var otpCode = GenerateOtpCode();
            await StoreOtpInDatabaseAsync(user.Id, otpCode);

            // Send email with OTP via MessagingApi
            await SendPasswordResetEmailAsync(user, otpCode, cancellationToken);

            return Success<string>("Password reset code sent successfully. Check your email.");
        }

        private string GenerateOtpCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Generates 100000-999999 (6 digits)
        }

        private async Task StoreOtpInDatabaseAsync(int userId, string otpCode)
        {
            // Delete any existing OTPs for this user
            var existingOtps = _context.PasswordResetOtps
                .Where(o => o.UserId == userId);
            _context.PasswordResetOtps.RemoveRange(existingOtps);

            // Create new OTP
            var otp = new PasswordResetOtp
            {
                UserId = userId,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5), // 5 minute expiry
                IsUsed = false
            };

            _context.PasswordResetOtps.Add(otp);
            await _context.SaveChangesAsync();
        }

        private async Task SendPasswordResetEmailAsync(User user, string otpCode, CancellationToken cancellationToken)
        {
            try
            {
                var messagingApiUrl = _configuration[MessagingApiBaseUrlKey];
                if (string.IsNullOrEmpty(messagingApiUrl))
                {
                    _logger.LogWarning("MessagingApi BaseUrl not configured. Reset email not sent.");
                    return;
                }

                var emailRequest = BuildPasswordResetEmailRequest(user, otpCode);
                await SendEmailRequestAsync(messagingApiUrl, emailRequest, cancellationToken);

                _logger.LogInformation("Password reset email queued for {Email}. User ID: {UserId}, OTP: {OtpCode}",
                    user.Email, user.Id, otpCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }
        }

        private object BuildPasswordResetEmailRequest(User user, string otpCode)
        {
            var emailSubject = "Password Reset Code - Qalam";
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 30px auto;
            background: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #dc3545 0%, #bd2130 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: bold;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content h2 {{
            color: #dc3545;
            margin-top: 0;
            font-size: 24px;
        }}
        .otp-code {{
            font-size: 48px;
            font-weight: bold;
            color: #dc3545;
            text-align: center;
            letter-spacing: 15px;
            padding: 30px;
            background: #f8f9fa;
            border-radius: 10px;
            margin: 30px 0;
            border: 2px dashed #dc3545;
        }}
        .warning {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .security-notice {{
            background: #f8d7da;
            border-left: 4px solid #dc3545;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔒 Qalam</h1>
        </div>
        
        <div class='content'>
            <h2>Password Reset Request</h2>
            <p>Hello {user.FirstName},</p>
            <p>We received a request to reset your password. Your password reset code is:</p>
            
            <div class='otp-code'>{otpCode}</div>
            
            <div class='warning'>
                <p><strong>⏰ Important:</strong> This code will expire in 5 minutes.</p>
            </div>
            
            <p><strong>To reset your password:</strong></p>
            <ol>
                <li>Go to the Reset Password page</li>
                <li>Enter your email: <strong>{user.Email}</strong></li>
                <li>Enter the 6-digit code shown above</li>
                <li>Choose your new password</li>
            </ol>
            
            <div class='security-notice'>
                <p><strong>⚠️ Security Alert:</strong></p>
                <p>If you didn't request this password reset, please ignore this email and ensure your account is secure.</p>
            </div>
        </div>
        
        <div class='footer'>
            <p>© 2024 Qalam. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return new
            {
                to = user.Email,
                subject = emailSubject,
                body = emailBody,
                isHtml = true,
                strategy = EmailSendingStrategy.Queued.ToIntValue()
            };
        }

        private async Task SendEmailRequestAsync(string baseUrl, object emailRequest, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var endpoint = $"{baseUrl}{MessagingApiEmailEndpoint}";
            var response = await httpClient.PostAsJsonAsync(endpoint, emailRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send password reset email via MessagingApi. Status: {StatusCode}",
                    response.StatusCode);
            }
        }
    }
}

