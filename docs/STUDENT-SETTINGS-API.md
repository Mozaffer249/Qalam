# Device tokens & account settings API

Single frontend guide for push device registration and student/guardian settings.

Base path prefix: `/Api/V1`

---

## 1. Device tokens on auth (all roles)

Optional body fields — best-effort; auth never fails if registration fails or fields are omitted.

| Flow | Endpoint |
|------|----------|
| Admin password login | `POST /Authentication/Admin/Login` |
| Teacher verify OTP | `POST /Authentication/Teacher/VerifyOtp` |
| Student verify OTP | `POST /Authentication/Student/VerifyOtp` |
| Refresh token | refresh body (same optional fields) |

```json
{
  "deviceToken": "<fcm-token>",
  "deviceTokenPlatform": "ios|android|web",
  "appVersion": "1.0.0"
}
```

If `deviceToken` is set and platform is omitted, platform defaults to **`android`**.

### Dedicated endpoints (after login)

| Method | Path | Auth | Body |
|--------|------|------|------|
| POST | `/Authentication/DeviceTokens` | JWT | `{ "token", "platform", "appVersion?" }` |
| DELETE | `/Authentication/DeviceTokens` | JWT | `{ "token" }` |

Use DELETE on logout. Use POST when FCM refreshes the token (`onTokenRefresh`).

---

## 2. Teacher web

Wired in `apps/teacher`:

- Helper: `src/lib/push/deviceToken.ts` → `resolveDeviceTokenPayload()`
- Call site: register `StepOTP` → `POST …/Teacher/VerifyOtp`
- Env (see `apps/teacher/.env.example`): `VITE_FIREBASE_API_KEY`, `VITE_FIREBASE_PROJECT_ID`, `VITE_FIREBASE_MESSAGING_SENDER_ID`, `VITE_FIREBASE_APP_ID`, `VITE_FIREBASE_VAPID_KEY` (+ optional authDomain / storageBucket / `VITE_APP_VERSION`)
- Service worker: `public/firebase-messaging-sw.js` — replace `REPLACE_ME` to match env

If Firebase env is missing, VerifyOtp continues **without** a token.

---

## 3. Admin web

Wired in `apps/admin`:

- Helper: `lib/push/deviceToken.ts` → `resolveDeviceTokenPayload()`
- Call site: `app/login/page.tsx` → `POST …/Admin/Login`
- Env (see `apps/admin/.env.example`): `NEXT_PUBLIC_FIREBASE_*` + `NEXT_PUBLIC_FIREBASE_VAPID_KEY`
- Service worker: `public/firebase-messaging-sw.js` — replace `REPLACE_ME` to match env

If Firebase env is missing, login continues **without** a token.

---

## 4. Student Flutter (implement in app)

Preferred: send FCM token on **Verify OTP**.

### Packages

- `firebase_core`, `firebase_messaging`
- `package_info_plus` (optional, for `appVersion`)

### Verify OTP

`POST /Api/V1/Authentication/Student/VerifyOtp` (no auth). Live path is **`VerifyOtp`** (not `VerifyOp`).

```json
{
  "phoneNumber": "503788444",
  "otpCode": "1234",
  "deviceToken": "<fcm-token>",
  "deviceTokenPlatform": "android",
  "appVersion": "1.0.0"
}
```

| Field | Required | Notes |
|-------|----------|--------|
| `phoneNumber` | yes | Digits without country code (same as SendOtp) |
| `otpCode` | yes | |
| `deviceToken` | no | From `FirebaseMessaging.instance.getToken()` |
| `deviceTokenPlatform` | no | `ios` or `android` |
| `appVersion` | no | From `PackageInfo.fromPlatform()` |

```dart
final fcmToken = await FirebaseMessaging.instance.getToken();
final info = await PackageInfo.fromPlatform();
final platform = Platform.isIOS ? 'ios' : 'android';

await api.post('/Api/V1/Authentication/Student/VerifyOtp', body: {
  'phoneNumber': phone,
  'otpCode': otp,
  if (fcmToken != null) 'deviceToken': fcmToken,
  if (fcmToken != null) 'deviceTokenPlatform': platform,
  'appVersion': info.version,
});
```

### Logout

```dart
await api.delete('/Api/V1/Authentication/DeviceTokens', body: {
  'token': fcmToken,
});
```

### Token refresh

Also call `POST /Authentication/DeviceTokens` from `FirebaseMessaging.instance.onTokenRefresh`.

---

## 5. Notification preferences (student/guardian)

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

Push delivery needs **`pushEnabled` and an active device token**.

---

## 6. Delete account (soft deactivate)

| Method | Path | Auth |
|--------|------|------|
| POST | `/Authentication/Delete` | JWT (Student or Guardian) |

```json
{ "password": "current-password" }
```

Sets `User.IsActive = false` (and linked Student/Guardian), revokes sessions, deactivates device tokens.

**Blocked (400)** when:

- Active / pending-payment enrollments → `ActiveEnrollment`
- Open session requests → `OpenSessionRequest`
- Pending S1/S2 invitations → `PendingInvitation`

Response `meta`:

```json
{ "blockingReasons": ["ActiveEnrollment", "OpenSessionRequest", "PendingInvitation"] }
```

Optional header: `X-Refresh-Token` for full session revoke.

---

## 7. FAQ

| Method | Path | Auth |
|--------|------|------|
| GET | `/Legal/Documents` | Anonymous |
| GET | `/Legal/Documents/faq` | Anonymous |

Seeded code: `faq`. `RequiresConsent: false`. Admin edits at **Admin → FAQ** (opens the legal document editor for `faq`).

---

## 8. Support / contact

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
