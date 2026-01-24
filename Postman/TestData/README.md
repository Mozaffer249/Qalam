# Test Data Files for E2E Testing
# ملفات بيانات الاختبار للاختبار الشامل

هذا المجلد يحتوي على الملفات المطلوبة لاختبار سيناريوهات المعلم والإدارة.

This folder contains the required files for testing teacher and admin scenarios.

---

## Required Files | الملفات المطلوبة

### Valid Files (for successful tests) | ملفات صالحة (للاختبارات الناجحة)

| File Name | Type | Size | Purpose |
|-----------|------|------|---------|
| `valid-national-id.pdf` | PDF | <10MB | Valid Saudi National ID document |
| `valid-iqama.pdf` | PDF | <10MB | Valid Iqama (residence permit) |
| `valid-passport.pdf` | PDF | <10MB | Valid Passport (for non-Saudi) |
| `valid-certificate-1.jpg` | JPEG | <10MB | Valid certificate image |
| `valid-certificate-2.png` | PNG | <10MB | Valid certificate image |
| `valid-certificate-3.pdf` | PDF | <10MB | Valid certificate PDF |

### Invalid Files (for negative tests) | ملفات غير صالحة (للاختبارات السلبية)

| File Name | Type | Size | Purpose |
|-----------|------|------|---------|
| `invalid-file.exe` | EXE | Any | Test invalid file type rejection |
| `invalid-file.txt` | TXT | Any | Test invalid file type rejection |
| `large-file.pdf` | PDF | >10MB | Test file size limit |

---

## How to Create Test Files | كيفية إنشاء ملفات الاختبار

### Option 1: Use Sample PDFs | الخيار 1: استخدام ملفات PDF نموذجية

1. Create a simple PDF with any content (text, image)
2. Rename it according to the required names above
3. Ensure file size is under 10MB

### Option 2: Use Online Tools | الخيار 2: استخدام أدوات عبر الإنترنت

- Use https://www.sejda.com/pdf-generator to create sample PDFs
- Use https://www.iloveimg.com/jpg-to-png for image conversion

### Option 3: Use Real Test Documents | الخيار 3: استخدام وثائق اختبار حقيقية

- Use scanned copies of real documents (with sensitive info redacted)

---

## File Placement for Postman | مكان وضع الملفات لـ Postman

When using Postman:
1. Open the request "Step 4: Upload Documents"
2. In the Body tab (form-data)
3. Click "Select Files" for each file field
4. Select the appropriate file from this folder

---

## Test Accounts | حسابات الاختبار

### Admin Account (Seeded) | حساب المسؤول (محمل مسبقاً)

```
Username: admin
Email: admin@qalam.com
Password: Admin@123456
Role: SuperAdmin
```

### Test Phone Numbers | أرقام هواتف الاختبار

Use any phone number starting with:
- `+966 5XXXXXXXX` (Saudi format)
- `+1 XXXXXXXXXX` (International format)

**Test OTP Code: `1234`** (always works in development)

---

## Environment Variables for Postman | متغيرات البيئة لـ Postman

Create a Postman environment with these variables:

| Variable | Initial Value | Description |
|----------|---------------|-------------|
| `base_url` | `http://localhost:5000` | API base URL |
| `teacher_token` | (empty) | Auto-filled after Step 2 |
| `admin_token` | (empty) | Filled after admin login |
| `teacher_id` | (empty) | Teacher ID for admin operations |
| `document_id` | (empty) | Document ID for approval/rejection |

---

## Quick Start Testing | البدء السريع في الاختبار

### 1. Start the API
```bash
cd Qalam.Api
dotnet run
```

### 2. Run Database Seeding
The database should be seeded with:
- Admin user (admin@qalam.com / Admin@123456)
- Roles (SuperAdmin, Admin, Teacher, Student, etc.)
- Education domains, levels, grades
- Quran data, teaching modes, etc.

### 3. Import Postman Collection
- Import `Qalam.postman_collection.json` from the project root
- Create an environment with the variables above
- Select the environment

### 4. Execute Test Flow

**Happy Path:**
1. Run "Step 1: Send OTP" with a new phone number
2. Run "Step 2: Verify OTP" with code `1234`
3. Run "Step 3: Complete Personal Info"
4. Run "Step 4: Upload Documents" with test files
5. Login as admin
6. Run "Get Pending Teachers"
7. Run "Get Teacher Details"
8. Run "Approve Document" for each document
9. Verify teacher status is "Active"

---

## Troubleshooting | استكشاف الأخطاء

### Common Issues | المشاكل الشائعة

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Token expired, re-authenticate |
| 400 Bad Request | Check request body format |
| File upload fails | Check file type and size |
| OTP invalid | Use test code `1234` |
| Teacher not found | Check teacher_id variable |

### API Not Running

```bash
# Check if API is running
curl http://localhost:5000/Api/V1/Health

# If not, start it
cd Qalam.Api
dotnet run
```

### Database Issues

```bash
# Reset and reseed database
dotnet ef database drop -f
dotnet ef database update
# Seeding runs automatically on startup
```
