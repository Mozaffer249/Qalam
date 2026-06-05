# Auth config API — frontend guide

Load authentication UI rules from the server before showing login or registration screens.

> **Teacher registration (steps 0–6, documents, admin):** everything is in **[Teacher-Registration-Guide.md](Teacher-Registration-Guide.md)** — this file covers **shared config API** and **student** OTP only.  
> **Scalar / Swagger:** tag **Authentication Config (Public)** for `GET /Api/V1/Authentication/Config`; **Admin · Auth Settings** for SuperAdmin GET/PUT.

## Step 0: load config

```http
GET /Api/V1/Authentication/Config
```

No auth header required.

### Response (`data`)

| Block | Use |
|-------|-----|
| `teacher` | Teacher app login/register |
| `student` | Student/parent app |
| `otp.length` | OTP input maxlength |
| `otp.expirySeconds` | Optional countdown |

### Persona fields

| Field | UI |
|-------|-----|
| `showPhoneField` | Show country code + phone |
| `showEmailField` | Show email on step 1 |
| `phoneRequired` | Validate phone before submit |
| `emailRequired` | Validate email before submit |
| `otpDelivery` | `"Email"` or `"Sms"` — drives copy |
| `otpHintEn` / `otpHintAr` | Subtitle on verify screen |
| `loginMethod` | `"Otp"` — show OTP flow (not password) |

### Example (email OTP, default seed)

```json
{
  "succeeded": true,
  "data": {
    "teacher": {
      "loginMethod": "Otp",
      "otpDelivery": "Email",
      "showPhoneField": true,
      "showEmailField": true,
      "phoneRequired": true,
      "emailRequired": true,
      "otpHintEn": "We sent a 4-digit code to your email",
      "otpHintAr": "أرسلنا رمزاً من 4 أرقام إلى بريدك الإلكتروني"
    },
    "student": { "...": "..." },
    "otp": { "length": 4, "expirySeconds": 300 }
  }
}
```

The server reads settings from the database on every request (no server-side cache). You may cache the config response in the app for a few minutes if you wish.

## Email OTP (teacher & student)

> **Teacher registration (full flow):** [Teacher-Registration-Guide.md](Teacher-Registration-Guide.md)

When `otpDelivery` is `"Email"` for the persona you are building:

| Step | Teacher app | Student/parent app |
|------|-------------|-------------------|
| 0 | `GET …/Authentication/Config` → `data.teacher` | Same → `data.student` |
| 1 | `POST …/Teacher/LoginOrRegister` (phone + email) | `POST …/Student/SendOtp` |
| 2 | User receives bilingual HTML email (EN + AR) from **Qalam Learning Platform** | Same |
| 3 | `POST …/Teacher/VerifyOtp` (phone + code) | `POST …/Student/VerifyOtp` |

**Server pipeline (email):**

1. Handler reads `teacher` or `student` settings from `Auth.Settings`.
2. `OtpService` generates code → `LoginOtpEmailTemplate` (teacher vs student copy).
3. Email queued on RabbitMQ → **Messaging API** sends via SMTP (`mail.dmail.sa:465`).

SMTP is configured in `.env` on **messaging-api** only (`EMAIL_*`). See `.env.example`.

## Send OTP (after config)

**Teacher:** `POST /Api/V1/Authentication/Teacher/LoginOrRegister`  
**Student:** `POST /Api/V1/Authentication/Student/SendOtp`

Body when `emailRequired` is true:

```json
{
  "countryCode": "+966",
  "phoneNumber": "501234567",
  "email": "user@example.com"
}
```

Response includes:

| Field | Meaning |
|-------|---------|
| `otpSentTo` | `"email"` or `"sms"` |
| `maskedDestination` | Where the code was sent |
| `isNewUser` | Registration vs login |

Use `otpHintEn` from config + `maskedDestination` on the verify screen.

## Admin: change settings (SuperAdmin)

```http
GET /Api/V1/Admin/SystemSettings/Auth
PUT /Api/V1/Admin/SystemSettings/Auth
```

PUT body matches stored JSON (`teacher`, `student`, `otp`). Setting `otpDelivery` to `"Sms"` requires `SmsSettings:Enabled` in server config.

## Settings storage

- Database: `common.SystemSettings`, key `Auth.Settings`, type JSON
- Fallback: `Auth:Settings` in `appsettings.json` if DB row missing
