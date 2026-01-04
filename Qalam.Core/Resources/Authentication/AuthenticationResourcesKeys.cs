namespace Qalam.Core.Resources.Authentication
{
    public static class AuthenticationResourcesKeys
    {
        // Authentication & Authorization
        public const string UserNameIsExist = "UserNameIsExist";
        public const string EmailIsExist = "EmailIsExist";
        public const string EmailIsNotExist = "EmailIsNotExist";
        public const string PhoneNumberIsExist = "PhoneNumberIsExist";
        public const string FailedToAddUser = "FailedToAddUser";
        public const string FailedToUpdateUser = "FailedToUpdateUser";
        public const string FailedToDeleteUser = "FailedToDeleteUser";
        public const string UserNotFound = "UserNotFound";
        public const string PasswordNotCorrect = "PasswordNotCorrect";
        public const string UserIsNotActive = "UserIsNotActive";
        public const string EmailNotConfirmed = "EmailNotConfirmed";
        public const string AccountLockedOut = "AccountLockedOut";
        public const string UserRegisteredSuccessfully = "UserRegisteredSuccessfully";
        public const string WelcomeEmailSubject = "WelcomeEmailSubject";
        public const string WelcomeEmailBody = "WelcomeEmailBody";

        // Two-Factor Authentication
        public const string TwoFactorCodeRequired = "TwoFactorCodeRequired";
        public const string InvalidTwoFactorCode = "InvalidTwoFactorCode";
        public const string TwoFactorEnabled = "TwoFactorEnabled";
        public const string TwoFactorDisabled = "TwoFactorDisabled";
        public const string TwoFactorNotEnabled = "TwoFactorNotEnabled";
        public const string RecoveryCodesGenerated = "RecoveryCodesGenerated";
        public const string InvalidRecoveryCode = "InvalidRecoveryCode";
        public const string TwoFactorCodeExpired = "TwoFactorCodeExpired";

        // Session Management
        public const string SessionTerminated = "SessionTerminated";
        public const string SessionNotFound = "SessionNotFound";
        public const string AllSessionsTerminated = "AllSessionsTerminated";
        public const string DeviceRemoved = "DeviceRemoved";
        public const string DeviceNotFound = "DeviceNotFound";

        // Password Security
        public const string PasswordInHistory = "PasswordInHistory";
        public const string WeakPassword = "WeakPassword";
        public const string PasswordExpired = "PasswordExpired";
        public const string PasswordChangedSuccessfully = "PasswordChangedSuccessfully";
        public const string CommonPasswordNotAllowed = "CommonPasswordNotAllowed";

        // Logout
        public const string LoggedOutSuccessfully = "LoggedOutSuccessfully";
        public const string LogoutFailed = "LogoutFailed";

        // Rate Limiting
        public const string TooManyLoginAttempts = "TooManyLoginAttempts";
        public const string TooManyRegistrationAttempts = "TooManyRegistrationAttempts";
        public const string TooManyPasswordResetAttempts = "TooManyPasswordResetAttempts";

        // Security Events
        public const string NewDeviceDetected = "NewDeviceDetected";
        public const string NewLocationDetected = "NewLocationDetected";
        public const string SuspiciousActivityDetected = "SuspiciousActivityDetected";

        // Account Management
        public const string ProfileUpdatedSuccessfully = "ProfileUpdatedSuccessfully";
        public const string EmailChangeRequested = "EmailChangeRequested";
        public const string EmailChangedSuccessfully = "EmailChangedSuccessfully";
        public const string InvalidEmailChangeToken = "InvalidEmailChangeToken";
        public const string EmailAlreadyInUse = "EmailAlreadyInUse";
        public const string SessionNotBelongsToUser = "SessionNotBelongsToUser";
        public const string DataExportGenerated = "DataExportGenerated";
        public const string AccountDeletedSuccessfully = "AccountDeletedSuccessfully";
        public const string PasswordRequiredForSensitiveAction = "PasswordRequiredForSensitiveAction";

        // Security Notifications
        public const string SecurityNotificationPasswordChanged = "SecurityNotificationPasswordChanged";
        public const string SecurityNotificationEmailChanged = "SecurityNotificationEmailChanged";
        public const string SecurityNotificationNewDevice = "SecurityNotificationNewDevice";
        public const string SecurityNotification2FAEnabled = "SecurityNotification2FAEnabled";
        public const string SecurityNotification2FADisabled = "SecurityNotification2FADisabled";

        // Field-Specific Validation Messages
        public const string UserNameIsRequired = "UserNameIsRequired";
        public const string UserNameOrEmailIsRequired = "UserNameOrEmailIsRequired";
        public const string PasswordIsRequired = "PasswordIsRequired";
        public const string PasswordMinLength = "PasswordMinLength";
        public const string FirstNameIsRequired = "FirstNameIsRequired";
        public const string LastNameIsRequired = "LastNameIsRequired";
        public const string EmailIsRequired = "EmailIsRequired";
        public const string EmailInvalidFormat = "EmailInvalidFormat";
        public const string ConfirmPasswordIsRequired = "ConfirmPasswordIsRequired";
        public const string UserNameMinLength = "UserNameMinLength";
        public const string UserNameMaxLength = "UserNameMaxLength";
        public const string FirstNameMaxLength = "FirstNameMaxLength";
        public const string LastNameMaxLength = "LastNameMaxLength";
        public const string PasswordsDoNotMatch = "PasswordsDoNotMatch";
        public const string TwoFactorCodeIsRequired = "TwoFactorCodeIsRequired";
        public const string TwoFactorCodeMustBeSixDigits = "TwoFactorCodeMustBeSixDigits";
    }
}

