namespace Qalam.Service.Models
{
    public class SecuritySettings
    {
        public SessionTimeoutSettings SessionTimeout { get; set; } = new SessionTimeoutSettings();
        public RiskBasedAuthSettings RiskBasedAuth { get; set; } = new RiskBasedAuthSettings();
        public EmailNotificationsSettings EmailNotifications { get; set; } = new EmailNotificationsSettings();

        public class SessionTimeoutSettings
        {
            public bool Enabled { get; set; } = true;
            public int InactivityMinutes { get; set; } = 480;
            public int CheckIntervalMinutes { get; set; } = 5;
        }

        public class RiskBasedAuthSettings
        {
            public bool Enabled { get; set; } = true;
            public int MaxFailedAttemptsPerIp { get; set; } = 10;
            public int FailedAttemptWindowMinutes { get; set; } = 60;
            public bool RequireTwoFactorForSuspiciousIp { get; set; } = true;
            public int BlockSuspiciousIpMinutes { get; set; } = 30;
        }

        public class EmailNotificationsSettings
        {
            public bool Enabled { get; set; } = true;
            public bool NotifyOnNewDeviceLogin { get; set; } = true;
            public bool NotifyOnPasswordChange { get; set; } = true;
            public bool NotifyOnSessionTerminated { get; set; } = true;
            public bool NotifyOnSuspiciousActivity { get; set; } = true;
        }
    }
}

