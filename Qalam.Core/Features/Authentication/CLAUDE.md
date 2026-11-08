# Authentication Feature

User registration, login, session management, and account security for all roles.

---

## Business rules

- **Registration (teacher):** Register → email OTP (4-digit, 5min expiry) → ConfirmEmail → active. Account starts `IsActive=false`.
- **Registration (student):** StudentSendOtp → VerifyOtpAndCreateAccount → CompleteStudentProfile / SetAccountTypeAndUsage. Separate flow from teacher.
- **Login:** risk assessment first (may block IP). Password check with lockout (5 failures). JWT + refresh token. Session created per device.
- **Password policy:** min 8 chars, upper+lower+digit+special. History check (last 5). Expiry at 90 days.
- **Sessions:** multi-device login. Trusted device tracking. Active sessions list with terminate.
- **Risk levels:** Low (known device/IP) → Medium (new device) → High (5-10 failures) → Critical (>10 failures, IP blocked 30min).
- **JWT:** access token + refresh token. Refresh via `RefreshTokenCommand`.
- **Localization:** all auth messages localized (en-US, ar-EG) via resource files.
- **Device tokens:** FCM push tokens registered on login, unregistered on logout. Platform: ios/android/web.
- **Teacher terms:** teachers must accept terms before proceeding (`AcceptTeacherTermsCommand`).

---

## Messages

### Commands

| Command | Description |
|---------|-------------|
| `RegisterCommand` | Teacher registration (email, password, name) |
| `StudentSendOtpCommand` | Student sends OTP to email/phone |
| `VerifyOtpAndCreateAccountCommand` | Student verifies OTP and creates account |
| `CompleteStudentProfileCommand` | Student completes profile after registration |
| `SetAccountTypeAndUsageCommand` | Student sets account type (self/guardian) |
| `ConfirmEmailCommand` | Verify email OTP code |
| `LoginCommand` | Login with email/username + password + device info |
| `LogoutCommand` | Logout (invalidate session) |
| `RefreshTokenCommand` | Refresh JWT using refresh token |
| `ChangePasswordCommand` | Change password (requires current) |
| `SendResetPasswordCodeCommand` | Send password reset OTP |
| `ResetPasswordCommand` | Reset password with OTP code |
| `SendPhoneOtpCommand` | Send phone verification OTP |
| `UpdateProfileCommand` | Update user profile fields |
| `UpdateMyProfilePictureCommand` | Upload/change profile picture |
| `CompletePersonalInfoCommand` | Complete personal info step |
| `UpdateNotificationPreferencesCommand` | Set email/push/SMS notification prefs |
| `RegisterDeviceTokenCommand` | Register FCM device token |
| `UnregisterDeviceTokenCommand` | Unregister FCM device token |
| `AcceptTeacherTermsCommand` | Teacher accepts platform terms |
| `DeactivateAccountCommand` | Deactivate own account |

### Queries

| Query | Description |
|-------|-------------|
| `GetAuthConfigQuery` | Public auth configuration (AllowAnonymous) |
| `GetProfileQuery` | Current user's profile |
| `GetActiveSessionsQuery` | List active login sessions |
| `GetTrustedDevicesQuery` | List trusted devices |
| `GetSecurityEventsQuery` | User's security event log |
| `GetNotificationPreferencesQuery` | Current notification preferences |
| `GetTeacherRegistrationRequirementsQuery` | Requirements for teacher registration |
| `ValidateTokenQuery` | Validate a JWT token |

---

## Key entities

- `User` (ASP.NET Identity) — `IsActive`, `EmailConfirmed`, roles
- `UserRefreshToken` — JWT refresh tokens per user
- `LoginSession` — per-device session (DeviceId, IP, IsTrusted, LastActivityAt)
- `TrustedDevice` — saved trusted devices
- `EmailConfirmationOtp` / `PasswordResetOtp` / `LoginOtp` / `PhoneConfirmationOtp` — OTP codes with expiry
- `AuditLog` / `SecurityEvent` — security audit trail
- `PasswordHistory` — last N password hashes
- `IpLoginAttempt` — rate limiting per IP
- `UserNotificationPreferences` — per-user notification channel prefs

---

## Gotchas

- `GetAuthConfigQuery` is `[AllowAnonymous]` — needed by mobile before login.
- Student and teacher registration are completely separate flows with different commands.
- Gregorian calendar is forced globally — `ar-EG` culture uses GregorianCalendar to prevent Hijri dates in DB.
- OTP codes are 4 digits, 5-minute expiry. Used for email confirm, password reset, phone verify, and login MFA.
- `DeactivateAccountCommand` has guard service that checks for active enrollments/sessions before allowing deactivation.
