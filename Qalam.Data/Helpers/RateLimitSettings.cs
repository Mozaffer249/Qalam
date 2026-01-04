namespace Qalam.Data.Helpers
{
    public class RateLimitSettings
    {
        public LoginSettings Login { get; set; } = new LoginSettings();
        public RegisterSettings Register { get; set; } = new RegisterSettings();
        public PasswordResetSettings PasswordReset { get; set; } = new PasswordResetSettings();
        public RefreshTokenSettings RefreshToken { get; set; } = new RefreshTokenSettings();

        public class LoginSettings
        {
            public int MaxAttempts { get; set; } = 5;
            public int WindowMinutes { get; set; } = 15;
        }

        public class RegisterSettings
        {
            public int MaxAttempts { get; set; } = 3;
            public int WindowMinutes { get; set; } = 60;
        }

        public class PasswordResetSettings
        {
            public int MaxAttempts { get; set; } = 3;
            public int WindowMinutes { get; set; } = 60;
        }

        public class RefreshTokenSettings
        {
            public int MaxAttempts { get; set; } = 10;
            public int WindowMinutes { get; set; } = 1;
        }
    }
}

