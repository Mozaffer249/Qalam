namespace Qalam.Data.AppMetaData
{
    public static class Router
    {
        public const string SignleRoute = "/{id}";

        public const string root = "Api";
        public const string version = "V1";
        public const string Rule = root + "/" + version + "/";

        #region Authentication
        public const string Authentication = Rule + "Authentication";
        public const string AuthenticationRegister = Authentication + "/Register";
        public const string AuthenticationLogin = Authentication + "/Login";
        public const string AuthenticationLoginWithTwoFactor = Authentication + "/LoginWithTwoFactor";
        public const string AuthenticationLogout = Authentication + "/Logout";
        public const string AuthenticationRefreshToken = Authentication + "/RefreshToken";
        public const string AuthenticationChangePassword = Authentication + "/ChangePassword";
        public const string AuthenticationSendResetPasswordCode = Authentication + "/SendResetPasswordCode";
        public const string AuthenticationResetPassword = Authentication + "/ResetPassword";
        public const string AuthenticationConfirmEmail = Authentication + "/ConfirmEmail";
        public const string AuthenticationValidateToken = Authentication + "/ValidateToken";
        public const string AuthenticationEnableTwoFactor = Authentication + "/EnableTwoFactor";
        public const string AuthenticationVerifyTwoFactor = Authentication + "/VerifyTwoFactor";
        public const string AuthenticationDisableTwoFactor = Authentication + "/DisableTwoFactor";
        public const string AuthenticationGenerateRecoveryCodes = Authentication + "/GenerateRecoveryCodes";
        public const string AuthenticationGetTwoFactorStatus = Authentication + "/GetTwoFactorStatus";

        // Account Management
        public const string AccountGetProfile = Authentication + "/Profile";
        public const string AccountUpdateProfile = Authentication + "/Profile/Update";
        public const string AccountChangeEmail = Authentication + "/ChangeEmail";
        public const string AccountConfirmEmailChange = Authentication + "/ConfirmEmailChange";
        public const string AccountGetSessions = Authentication + "/Sessions";
        public const string AccountTerminateSession = Authentication + "/Sessions/Terminate";
        public const string AccountTerminateAllSessions = Authentication + "/Sessions/TerminateAll";
        public const string AccountGetTrustedDevices = Authentication + "/TrustedDevices";
        public const string AccountTrustDevice = Authentication + "/TrustDevice";
        public const string AccountRemoveTrustedDevice = Authentication + "/TrustedDevices/Remove";
        public const string AccountGetSecurityEvents = Authentication + "/SecurityEvents";
        public const string AccountExportData = Authentication + "/ExportData";
        public const string AccountDelete = Authentication + "/Delete";
        #endregion

        #region Users
        public const string Users = Rule + "Users";
        public const string UserById = Users + SignleRoute;
        #endregion
    }
}

