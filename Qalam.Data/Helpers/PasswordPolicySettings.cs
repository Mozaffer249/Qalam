using System.Collections.Generic;

namespace Qalam.Data.Helpers
{
    public class PasswordPolicySettings
    {
        public int MinimumLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public int PreventPasswordReuse { get; set; } = 5;
        public int PasswordExpiryDays { get; set; } = 90;
        public bool CheckCommonPasswords { get; set; } = true;
    }

    public class PasswordStrength
    {
        public int Score { get; set; } // 0-4
        public List<string> Feedback { get; set; } = new List<string>();
        public bool IsStrong => Score >= 3;
    }
}

