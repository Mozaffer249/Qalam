# Student Settings API

Frontend integration guide for student/guardian account settings endpoints.

Base path prefix: `/Api/V1`

## Notification preferences

Local toggles alone are not enough — persist via these endpoints.

| Method | Path | Auth |
|--------|------|------|
| GET | `/Authentication/NotificationPreferences` | JWT |
| PUT | `/Authentication/NotificationPreferences` | JWT |

```json
{
  "pushEnabled": true,
  "emailDigestEnabled": true,
  "smsAlertsEnabled": false
}
```

Defaults on first GET: push on, email digest on, SMS alerts off.

Push delivery also requires a registered device token (below). Prefer checking both `pushEnabled` and an active token before expecting pushes.

---

## Device tokens (push)

Register during auth (preferred) or via dedicated endpoint after login.

### On login / verify OTP (all roles)

Optional fields on:

| Flow | Endpoint |
|------|----------|
| Admin password login | `POST /Authentication/Admin/Login` |
| Teacher verify OTP | `POST /Authentication/Teacher/VerifyOtp` |
| Student verify OTP | `POST /Authentication/Student/VerifyOtp` |
| Refresh token | refresh endpoint body (same optional fields) |

```json
{
  "deviceToken": "<fcm-token>",
  "deviceTokenPlatform": "ios|android|web",
  "appVersion": "1.0.0"
}
```

If `deviceToken` is sent and platform is omitted, platform defaults to `android`. Registration is best-effort and does not fail auth.

### Dedicated endpoint

| Method | Path | Auth |
|--------|------|------|
| POST | `/Authentication/DeviceTokens` | JWT |
| DELETE | `/Authentication/DeviceTokens` | JWT |

Register:

```json
{ "token": "<fcm-token>", "platform": "ios|android|web", "appVersion": "1.0.0" }
```

Unregister (logout / disable push):

```json
{ "token": "<fcm-token>" }
```

---

## Delete account (soft deactivate)

| Method | Path | Auth |
|--------|------|------|
| POST | `/Authentication/Delete` | JWT (Student or Guardian) |

```json
{ "password": "current-password" }
```

Sets `User.IsActive = false` (and linked Student/Guardian), revokes sessions, deactivates device tokens.

**Blocked (400)** when the account has:

- Active or pending-payment enrollments → `ActiveEnrollment`
- Open session requests (`StudentOpen` + `OfferAccepted`) → `OpenSessionRequest`
- Pending S1/S2 invitations (received or sent) → `PendingInvitation`

Response `meta`:

```json
{ "blockingReasons": ["ActiveEnrollment", "OpenSessionRequest", "PendingInvitation"] }
```

Optional header: `X-Refresh-Token` for full session revoke.

---

## FAQ

Reuse legal documents API — no dedicated FAQ controller.

| Method | Path | Auth |
|--------|------|------|
| GET | `/Legal/Documents` | Anonymous |
| GET | `/Legal/Documents/faq` | Anonymous |

Seeded code: `faq` (bilingual Q&A sections). `RequiresConsent: false`.

---

## Support / contact

| Method | Path | Auth |
|--------|------|------|
| POST | `/Contact` | Anonymous (JWT optional) |

```json
{
  "name": "optional when JWT",
  "phone": "optional when JWT",
  "email": "optional",
  "reason": "TechnicalSupport",
  "message": "…"
}
```

When JWT is present and name/phone/email are omitted, the API prefills from the authenticated profile.

Valid `reason` values: `GeneralInquiry`, `TechnicalSupport`, `Partnership`, `TeachingApplication`, `Other`.
