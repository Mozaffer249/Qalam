# Teacher Registration Documentation

## Overview

The teacher registration system implements a secure 4-step phone-based registration process. Teachers verify their phone number, provide personal information, and upload required documents for admin verification.

## Table of Contents

- [Architecture](#architecture)
- [Registration Flow](#registration-flow)
- [API Endpoints](#api-endpoints)
- [Security Features](#security-features)
- [Business Rules](#business-rules)
- [Error Handling](#error-handling)
- [Testing Guide](#testing-guide)

---

## Architecture

### Design Patterns

- **Clean Architecture** - Clear separation of concerns across layers
- **CQRS Pattern** - Commands for all registration operations
- **MediatR Pipeline** - Automatic user identity injection via `UserIdentityBehavior`
- **Repository Pattern** - Data access abstraction
- **Service Layer** - Business logic encapsulation

### Key Components

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                             │
│  AuthController - 4 endpoints for registration steps         │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                   Application Layer (Core)                   │
│  - SendPhoneOtpCommand                                       │
│  - VerifyOtpAndCreateAccountCommand                          │
│  - CompletePersonalInfoCommand (IAuthenticatedRequest)       │
│  - UploadTeacherDocumentsCommand (IAuthenticatedRequest)     │
│                                                               │
│  Pipeline Behaviors:                                         │
│  → UserIdentityBehavior (extracts UserId from JWT)           │
│  → ValidationBehavior (validates commands)                   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                     Service Layer                            │
│  - OtpService (OTP generation & verification)                │
│  - TeacherRegistrationService (account management)           │
│  - FileStorageService (document uploads)                     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  - PhoneOtpRepository                                        │
│  - TeacherRepository                                         │
│  - TeacherDocumentRepository                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                      Database                                │
│  - PhoneConfirmationOtps                                     │
│  - Users                                                     │
│  - Teachers                                                  │
│  - TeacherDocuments                                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Registration Flow

### Complete Process Diagram

```mermaid
sequenceDiagram
    participant T as Teacher
    participant API as API
    participant Auth as Authentication
    participant DB as Database
    
    Note over T,DB: Step 1: Phone Verification
    T->>API: POST /Teacher/Step1-SendOtp<br/>{countryCode, phoneNumber}
    API->>Auth: Generate 4-digit OTP
    Auth->>DB: Store OTP (5 min expiry)
    Auth->>T: SMS with OTP code
    
    Note over T,DB: Step 2: OTP Verification & Account Creation
    T->>API: POST /Teacher/Step2-VerifyOtp<br/>{phoneNumber, otpCode}
    API->>Auth: Verify OTP
    Auth->>DB: Create User (basic)<br/>PhoneNumberConfirmed=true<br/>IsActive=false
    Auth->>DB: Assign "Teacher" role
    Auth->>T: Return JWT Token + UserId
    
    Note over T,DB: Step 3: Personal Information
    T->>API: POST /Teacher/Step3-PersonalInfo<br/>Authorization: Bearer {token}<br/>{firstName, lastName, email, password}
    API->>Auth: Extract UserId from JWT (automatic)
    Auth->>DB: Update User (name, email, password)<br/>IsActive=true
    Auth->>DB: Create Teacher Profile<br/>Status=AwaitingDocuments
    Auth->>T: Return TeacherId + New Token
    
    Note over T,DB: Step 4: Document Upload
    T->>API: POST /Teacher/Step4-UploadDocuments<br/>Authorization: Bearer {token}<br/>{documents, certificates}
    API->>Auth: Extract UserId from JWT (automatic)
    Auth->>DB: Get Teacher by UserId
    Auth->>DB: Save Identity Document
    Auth->>DB: Save Certificates (1-5)
    Auth->>DB: Update Teacher Status=PendingVerification
    Auth->>T: Registration Complete<br/>Awaiting Admin Verification
```

---

## Registration Status Tracking

### Teacher Status Flow

The system tracks teacher registration progress using the `TeacherStatus` enum. This allows the application to determine what step a user should resume when they login with an incomplete registration.

#### Status Definitions

| Status | Numeric Value | Meaning | Next Action |
|--------|---------------|---------|-------------|
| `AwaitingDocuments` | 1 | Step 3 completed - Personal info added | User should proceed to Step 4 (Upload Documents) |
| `PendingVerification` | 2 | Step 4 completed - Documents uploaded | User should wait for admin approval |
| `Active` | 3 | Admin approved - Fully verified | Teacher can start teaching |
| `Blocked` | 4 | Account blocked by admin | Cannot access system |

#### State Mapping by Registration Step

| Step Completed | User Account | Teacher Profile | Teacher.Status | Next Step Required |
|----------------|--------------|-----------------|----------------|-------------------|
| Step 1 (OTP Sent) | No | No | N/A | Verify OTP (Step 2) |
| Step 2 (OTP Verified) | Yes | No | N/A | Complete Personal Info (Step 3) |
| Step 3 (Personal Info) | Yes | Yes | `AwaitingDocuments` | Upload Documents (Step 4) |
| Step 4 (Documents) | Yes | Yes | `PendingVerification` | Wait for Admin Approval |
| Admin Approved | Yes | Yes | `Active` | Can start teaching |

### Checking Registration Progress

Use the `GetNextRegistrationStepAsync` method to determine where a user left off:

**Service Method:**

```csharp
Task<RegistrationStepDto> GetNextRegistrationStepAsync(int userId);
```

**Response DTO:**

```csharp
public class RegistrationStepDto
{
    public int CurrentStep { get; set; }        // Last completed step (2, 3, or 4)
    public int NextStep { get; set; }            // Next step to complete (3, 4, or 0 if done)
    public string NextStepName { get; set; }     // Human-readable next action
    public bool IsRegistrationComplete { get; set; }
    public string? Message { get; set; }         // Optional additional info
}
```

### Example: Incomplete Registration Scenarios

#### Scenario 1: User stopped after Step 2 (OTP verification)

```csharp
// User has account but NO Teacher profile
var step = await _service.GetNextRegistrationStepAsync(userId);
// Returns:
{
    "currentStep": 2,
    "nextStep": 3,
    "nextStepName": "Complete Personal Information",
    "isRegistrationComplete": false,
    "message": null
}
```

**Redirect user to:** Step 3 - Personal Information Form

#### Scenario 2: User stopped after Step 3 (Personal info)

```csharp
// User has account AND Teacher profile with Status = AwaitingDocuments
var step = await _service.GetNextRegistrationStepAsync(userId);
// Returns:
{
    "currentStep": 3,
    "nextStep": 4,
    "nextStepName": "Upload Documents",
    "isRegistrationComplete": false,
    "message": null
}
```

**Redirect user to:** Step 4 - Document Upload Form

#### Scenario 3: User completed Step 4 (Documents uploaded)

```csharp
// Teacher profile with Status = PendingVerification
var step = await _service.GetNextRegistrationStepAsync(userId);
// Returns:
{
    "currentStep": 4,
    "nextStep": 0,
    "nextStepName": "Awaiting Admin Verification",
    "isRegistrationComplete": false,
    "message": "Your documents are being reviewed by our team."
}
```

**Show user:** "Registration complete, pending admin approval" message

#### Scenario 4: Admin approved the teacher

```csharp
// Teacher profile with Status = Active
var step = await _service.GetNextRegistrationStepAsync(userId);
// Returns:
{
    "currentStep": 4,
    "nextStep": 0,
    "nextStepName": "Registration Complete",
    "isRegistrationComplete": true,
    "message": null
}
```

**Allow user to:** Access full teacher dashboard

### Login Flow with Status Check

When a teacher tries to login, check their registration status:

```csharp
// 1. Authenticate user
var user = await _userManager.FindByNameAsync(username);
if (user == null) 
    return BadRequest("Invalid credentials");

var passwordValid = await _userManager.CheckPasswordAsync(user, password);
if (!passwordValid)
    return BadRequest("Invalid credentials");

// 2. Check registration status
var registrationStep = await _teacherRegistrationService
    .GetNextRegistrationStepAsync(user.Id);

// 3. Handle incomplete registration
if (!registrationStep.IsRegistrationComplete)
{
    return new
    {
        Token = GenerateJWT(user),
        RegistrationIncomplete = true,
        CurrentStep = registrationStep.CurrentStep,
        NextStep = registrationStep.NextStep,
        NextStepName = registrationStep.NextStepName,
        Message = registrationStep.Message
    };
}

// 4. Normal login for complete registration
return new { Token = GenerateJWT(user), User = userDto };
```

### Frontend Implementation

**Step Navigation State:**

```javascript
const useRegistrationStatus = () => {
  const [registrationStep, setRegistrationStep] = useState(null);
  
  const checkStatus = async () => {
    try {
      const response = await api.get('/Teacher/RegistrationStatus');
      setRegistrationStep(response.data);
      
      // Redirect based on status
      if (!response.data.isRegistrationComplete) {
        switch (response.data.nextStep) {
          case 3:
            router.push('/register/personal-info');
            break;
          case 4:
            router.push('/register/documents');
            break;
          case 0:
            if (response.data.nextStepName.includes('Verification')) {
              router.push('/register/pending-approval');
            }
            break;
        }
      }
    } catch (error) {
      console.error('Failed to check registration status', error);
    }
  };
  
  return { registrationStep, checkStatus };
};
```

---

## API Endpoints

### Base URL

```
https://api.qalam.com/Api/V1/Authentication/Teacher
```

---

### Step 1: Send OTP to Phone

**Endpoint:** `POST /Teacher/Step1-SendOtp`

**Authentication:** None required

**Request Body:**

```json
{
  "countryCode": "+966",
  "phoneNumber": "512345678"
}
```

**Response (200 OK):**

```json
{
  "data": null,
  "succeeded": true,
  "message": "OTP sent successfully. Please check your phone.",
  "statusCode": 200
}
```

**Validation Rules:**

- `phoneNumber`: Required, 9-15 digits
- `countryCode`: Required, format: `+[1-4 digits]`

**Business Logic:**

1. Validates phone number format
2. Checks phone number not already registered
3. Generates 4-digit OTP code
4. Stores OTP in database (5-minute expiry)
5. Sends SMS to phone number
6. Returns success message

**Error Responses:**

```json
// Phone already registered
{
  "succeeded": false,
  "message": "Phone number already registered",
  "statusCode": 400
}

// Invalid phone format
{
  "succeeded": false,
  "message": "Phone number must be 9-15 digits",
  "statusCode": 400
}
```

---

### Step 2: Verify OTP and Create Account

**Endpoint:** `POST /Teacher/Step2-VerifyOtp`

**Authentication:** None required

**Request Body:**

```json
{
  "phoneNumber": "512345678",
  "otpCode": "1234"
}
```

**Response (200 OK):**

```json
{
  "data": {
    "userId": 42,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "phoneNumber": "+966512345678"
  },
  "succeeded": true,
  "message": "Success",
  "statusCode": 200
}
```

**Validation Rules:**

- `phoneNumber`: Required
- `otpCode`: Required, exactly 4 digits

**Business Logic:**

1. Verifies OTP is valid and not expired
2. Creates User account:
   - UserName = full phone number
   - PhoneNumber = countryCode + phoneNumber
   - PhoneNumberConfirmed = true
   - IsActive = false (activated in Step 3)
3. Assigns "Teacher" role
4. Generates JWT token
5. Marks OTP as used
6. Returns user ID and token

**Error Responses:**

```json
// Invalid or expired OTP
{
  "succeeded": false,
  "message": "Invalid or expired OTP code",
  "statusCode": 400
}

// OTP already used
{
  "succeeded": false,
  "message": "OTP verification failed",
  "statusCode": 400
}
```

**Important Notes:**

- Save the returned `token` - required for Steps 3 & 4
- OTP expires after 5 minutes
- Each OTP can only be used once

---

### Step 3: Complete Personal Information

**Endpoint:** `POST /Teacher/Step3-PersonalInfo`

**Authentication:** **Required** - Bearer token from Step 2

**Headers:**

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**

```json
{
  "firstName": "أحمد",
  "lastName": "محمد",
  "email": "ahmed.mohamed@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**

```json
{
  "data": {
    "userId": 42,
    "teacherId": 15,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  },
  "succeeded": true,
  "message": "Success",
  "statusCode": 200
}
```

**Validation Rules:**

- `firstName`: Required, max 50 characters
- `lastName`: Required, max 50 characters
- `email`: Optional, valid email format
- `password`: Required, min 8 characters, must contain:
  - At least 1 uppercase letter
  - At least 1 lowercase letter
  - At least 1 number
  - At least 1 special character

**Business Logic:**

1. **Automatic**: UserId extracted from JWT token (via `UserIdentityBehavior`)
2. Updates User account:
   - FirstName, LastName, Email
   - Password (hashed)
   - IsActive = true
3. Creates Teacher profile:
   - UserId link
   - Status = Pending
4. Generates new JWT token (with updated info)
5. Returns teacher ID and new token

**Security Features:**

- `UserId` automatically extracted from JWT - **cannot be manipulated**
- `[BindNever]` attribute prevents client from sending userId
- User can only update their own account

**Error Responses:**

```json
// Weak password
{
  "succeeded": false,
  "message": "Password must contain at least one uppercase letter",
  "statusCode": 400
}

// Invalid token
{
  "succeeded": false,
  "message": "User not authenticated",
  "statusCode": 401
}
```

**Important Notes:**

- Save the new `token` - required for Step 4
- Save the `teacherId` for reference

---

### Step 4: Upload Documents

**Endpoint:** `POST /Teacher/Step4-UploadDocuments`

**Authentication:** **Required** - Bearer token from Step 3 + Teacher role

**Headers:**

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data
```

**Request Body (Form Data):**

```
documents.isInSaudiArabia: true

// Identity Document
documents.identityDocument.identityType: 1  // 1=NationalId, 2=Iqama, 3=Passport
documents.identityDocument.documentNumber: 1234567890
documents.identityDocument.issuingCountryCode: (empty for NationalId/Iqama, required for Passport)
documents.identityDocument.file: [File]

// Certificates (1-5 files)
documents.certificates[0].file: [File]
documents.certificates[0].title: "Bachelor's Degree in Education"
documents.certificates[0].issuer: "King Saud University"
documents.certificates[0].issueDate: 2020-06-15

documents.certificates[1].file: [File]
documents.certificates[1].title: "Teaching Certificate"
documents.certificates[1].issuer: "Ministry of Education"
documents.certificates[1].issueDate: 2021-09-01
```

**Response (200 OK):**

```json
{
  "data": null,
  "succeeded": true,
  "message": "Registration completed successfully. Your documents are pending verification.",
  "statusCode": 200
}
```

**Identity Types:**

- `1` = NationalId (Saudi National ID)
- `2` = Iqama (Saudi Residence Permit)
- `3` = Passport (International Passport)

**File Requirements:**

- **Allowed formats**: PDF, JPG, JPEG, PNG
- **Max file size**: 10 MB per file
- **Identity document**: 1 file required
- **Certificates**: 1-5 files required

**Validation Rules:**

**If Inside Saudi Arabia (`isInSaudiArabia: true`):**

- Identity Type must be **NationalId** or **Iqama**
- Passport **not allowed**
- `issuingCountryCode` must be empty

**If Outside Saudi Arabia (`isInSaudiArabia: false`):**

- Identity Type must be **Passport**
- NationalId and Iqama **not allowed**
- `issuingCountryCode` **required** (ISO 3166-1 alpha-3, e.g., "USA", "EGY")

**Business Logic:**

1. **Automatic**: UserId extracted from JWT token
2. Retrieves Teacher profile by UserId
3. Validates identity document rules (Saudi vs International)
4. Validates file types and sizes
5. Checks identity document number uniqueness
6. Saves identity document file to storage
7. Saves certificates files to storage
8. Stores document metadata in database
9. Updates Teacher status to **PendingVerification**
10. Updates Teacher location (InsideSaudiArabia / OutsideSaudiArabia)

**Security Features:**

- `UserId` automatically extracted from JWT - **cannot be manipulated**
- Teacher can only upload to their own profile
- File validation prevents malicious uploads
- Identity uniqueness prevents duplicate registrations

**Error Responses:**

```json
// Passport used inside Saudi Arabia
{
  "succeeded": false,
  "message": "Passport is not allowed for teachers inside Saudi Arabia. Please use National ID or Iqama.",
  "statusCode": 400
}

// File too large
{
  "succeeded": false,
  "message": "Identity document file is invalid or too large",
  "statusCode": 400
}

// Identity already registered
{
  "succeeded": false,
  "message": "This identity document is already registered in the system.",
  "statusCode": 400
}

// Wrong number of certificates
{
  "succeeded": false,
  "message": "At least 1 certificate is required",
  "statusCode": 400
}

{
  "succeeded": false,
  "message": "Maximum 5 certificates allowed",
  "statusCode": 400
}

// Teacher profile not found
{
  "succeeded": false,
  "message": "Teacher profile not found. Please complete Step 3 first.",
  "statusCode": 400
}
```

**File Storage:**

```
uploads/teachers/{teacherId}/identity/{guid}.pdf
uploads/teachers/{teacherId}/certificates/{guid}.jpg
```

---

## Security Features

### 1. Phone Verification

- 4-digit OTP with 5-minute expiry
- One-time use only
- Cleaned up after use or expiry

### 2. JWT Token Authentication

- Token required for Steps 3 & 4
- Token contains encrypted user claims
- Token refreshed after Step 3 with updated info

### 3. Automatic User Identity Injection

- **UserIdentityBehavior** MediatR pipeline
- Extracts UserId from JWT token automatically
- Prevents parameter tampering
- `[BindNever]` attribute blocks client manipulation

### 4. Role-Based Authorization

- Step 3: Requires authentication
- Step 4: Requires authentication + "Teacher" role

### 5. Input Validation

- FluentValidation for all commands
- File type and size validation
- Business rule validation

### 6. Data Protection

- Passwords hashed using ASP.NET Core Identity
- OTP codes stored temporarily only
- User must be authenticated to proceed

---

## Business Rules

### Phone Number Rules

- Must be unique across the system
- Once registered, cannot register again with same phone
- Phone number becomes the initial username

### OTP Rules

- 4-digit numeric code
- Valid for 5 minutes
- Single use only
- Old OTPs cleaned up when new one generated

### Identity Document Rules

**For Teachers Inside Saudi Arabia:**

- ✅ Can use: National ID, Iqama
- ❌ Cannot use: Passport
- No country code required

**For Teachers Outside Saudi Arabia:**

- ✅ Can use: Passport only
- ❌ Cannot use: National ID, Iqama
- Country code required (ISO 3166-1 alpha-3)

**Uniqueness:**

- Each identity document number can only be registered once
- Uniqueness checked by: (IdentityType + DocumentNumber + IssuingCountryCode)

### Certificate Rules

- Minimum: 1 certificate
- Maximum: 5 certificates
- Each certificate can have optional metadata (title, issuer, issue date)

### Teacher Status Flow

```
Pending (after Step 3)
    ↓
PendingVerification (after Step 4)
    ↓
Active (after admin approval) OR Blocked (if rejected)
```

---

## Error Handling

### Common HTTP Status Codes

- **200 OK** - Request successful
- **400 Bad Request** - Validation failed or business rule violated
- **401 Unauthorized** - Missing or invalid token
- **403 Forbidden** - Insufficient permissions
- **404 Not Found** - Resource doesn't exist
- **500 Internal Server Error** - Server error

### Error Response Format

```json
{
  "data": null,
  "succeeded": false,
  "message": "Error description here",
  "statusCode": 400
}
```

---

## Testing Guide

### Test Scenario 1: Successful Registration (Saudi Teacher)

**Step 1:**

```bash
POST /Teacher/Step1-SendOtp
{
  "countryCode": "+966",
  "phoneNumber": "512345678"
}
```

Check your SMS for the OTP code.

**Step 2:**

```bash
POST /Teacher/Step2-VerifyOtp
{
  "phoneNumber": "512345678",
  "otpCode": "1234"
}
```

Save the `token` from response.

**Step 3:**

```bash
POST /Teacher/Step3-PersonalInfo
Authorization: Bearer {token_from_step_2}
{
  "firstName": "أحمد",
  "lastName": "محمد",
  "email": "ahmed@example.com",
  "password": "SecurePass123!"
}
```

Save the new `token` from response.

**Step 4:**

```bash
POST /Teacher/Step4-UploadDocuments
Authorization: Bearer {token_from_step_3}
Content-Type: multipart/form-data

documents.isInSaudiArabia: true
documents.identityDocument.identityType: 1
documents.identityDocument.documentNumber: 1234567890
documents.identityDocument.file: [NationalID.pdf]
documents.certificates[0].file: [Degree.pdf]
documents.certificates[0].title: "Bachelor's in Education"
```

Expected: Success message, teacher status = PendingVerification

---

### Test Scenario 2: International Teacher (Egypt)

Same as above, but in Step 4:

```bash
documents.isInSaudiArabia: false
documents.identityDocument.identityType: 3  // Passport
documents.identityDocument.documentNumber: A12345678
documents.identityDocument.issuingCountryCode: EGY  // ISO 3166-1 alpha-3
documents.identityDocument.file: [Passport.pdf]
```

---

### Test Scenario 3: Invalid Scenarios

**Test: Phone Already Registered**

- Try Step 1 with already registered phone
- Expected: 400 error

**Test: Invalid OTP**

- Try Step 2 with wrong OTP code
- Expected: 400 error

**Test: Expired OTP**

- Wait 6 minutes after Step 1
- Try Step 2
- Expected: 400 error

**Test: Weak Password**

- Step 3 with password "test123"
- Expected: 400 error with validation messages

**Test: Wrong Identity Type**

- Step 4: Inside Saudi Arabia + Passport
- Expected: 400 error

**Test: Missing Certificates**

- Step 4: Upload identity but 0 certificates
- Expected: 400 error

**Test: Too Many Certificates**

- Step 4: Upload 6 certificates
- Expected: 400 error

**Test: Duplicate Identity Document**

- Complete registration with National ID 1234567890
- Try another registration with same National ID
- Expected: 400 error at Step 4

---

## Database Schema

### PhoneConfirmationOtps

```sql
- Id (int, PK)
- CountryCode (varchar(5))
- PhoneNumber (varchar(20))
- OtpCode (varchar(4))
- CreatedAt (datetime)
- ExpiresAt (datetime)
- IsUsed (bit)
- UsedAt (datetime, nullable)
- UserId (int, nullable, FK to Users)
```

### Users (ASP.NET Identity)

```sql
- Id (int, PK)
- UserName (varchar)
- PhoneNumber (varchar)
- PhoneNumberConfirmed (bit)
- Email (varchar, nullable)
- FirstName (varchar)
- LastName (varchar)
- IsActive (bit)
- PasswordHash (varchar)
```

### Teachers

```sql
- Id (int, PK)
- UserId (int, FK to Users)
- Bio (varchar(500), nullable)
- Status (int) -- Pending=1, Active=2, Blocked=3, PendingVerification=4
- Location (int, nullable) -- InsideSaudiArabia=1, OutsideSaudiArabia=2
- RatingAverage (decimal)
- IsActive (bit)
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

### TeacherDocuments

```sql
- Id (int, PK)
- TeacherId (int, FK to Teachers)
- DocumentType (int) -- NationalId=4, Iqama=5, Passport=6, Certificate=2
- FilePath (varchar(400))
- DocumentNumber (varchar(50), nullable)
- IdentityType (int, nullable) -- NationalId=1, Iqama=2, Passport=3
- IssuingCountryCode (varchar(3), nullable)
- CertificateTitle (varchar(200), nullable)
- Issuer (varchar(200), nullable)
- IssueDate (date, nullable)
- IsVerified (bit)
- VerifiedByAdminId (int, nullable)
- VerifiedAt (datetime, nullable)
- RejectionReason (varchar(500), nullable)
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

---

## Frontend Integration Guide

### State Management

Store these values during registration:

```javascript
// After Step 2
const registrationState = {
  userId: 42,
  token: "eyJhbG...",
  phoneNumber: "+966512345678"
};

// After Step 3 (update token)
registrationState.token = "newToken...";
registrationState.teacherId = 15;
```

### Step Navigation

```javascript
const steps = [
  { id: 1, title: "Phone Verification", completed: false },
  { id: 2, title: "OTP Verification", completed: false },
  { id: 3, title: "Personal Info", completed: false },
  { id: 4, title: "Documents", completed: false }
];
```

### Example: React Hook

```javascript
const useTeacherRegistration = () => {
  const [currentStep, setCurrentStep] = useState(1);
  const [token, setToken] = useState(null);
  const [teacherId, setTeacherId] = useState(null);

  const sendOtp = async (countryCode, phoneNumber) => {
    const response = await api.post('/Teacher/Step1-SendOtp', {
      countryCode,
      phoneNumber
    });
    if (response.succeeded) {
      setCurrentStep(2);
    }
    return response;
  };

  const verifyOtp = async (phoneNumber, otpCode) => {
    const response = await api.post('/Teacher/Step2-VerifyOtp', {
      phoneNumber,
      otpCode
    });
    if (response.succeeded) {
      setToken(response.data.token);
      setCurrentStep(3);
    }
    return response;
  };

  const completePersonalInfo = async (data) => {
    const response = await api.post('/Teacher/Step3-PersonalInfo', data, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (response.succeeded) {
      setToken(response.data.token);
      setTeacherId(response.data.teacherId);
      setCurrentStep(4);
    }
    return response;
  };

  const uploadDocuments = async (formData) => {
    const response = await api.post('/Teacher/Step4-UploadDocuments', formData, {
      headers: { 
        Authorization: `Bearer ${token}`,
        'Content-Type': 'multipart/form-data'
      }
    });
    return response;
  };

  return {
    currentStep,
    token,
    teacherId,
    sendOtp,
    verifyOtp,
    completePersonalInfo,
    uploadDocuments
  };
};
```

---

## Admin Verification Process

After Step 4, teachers enter **PendingVerification** status. Admins must:

1. Review identity document
2. Verify certificates
3. Either:
   - **Approve**: Set status to Active
   - **Reject**: Set status to Blocked with rejection reason

*(Admin endpoints documented separately)*

---

## FAQs

**Q: Can I use the same phone number for multiple accounts?**  
A: No, each phone number can only be registered once.

**Q: How long is the OTP valid?**  
A: 5 minutes from generation time.

**Q: What if I enter the wrong OTP?**  
A: You can request a new OTP by repeating Step 1.

**Q: Can I upload documents later?**  
A: No, all 4 steps must be completed in sequence. Step 4 is required to complete registration.

**Q: What file formats are accepted?**  
A: PDF, JPG, JPEG, and PNG files, max 10MB each.

**Q: How many certificates do I need?**  
A: Minimum 1, maximum 5 certificates.

**Q: I'm a Saudi teacher abroad. Which identity document do I use?**  
A: If you select "Outside Saudi Arabia", you must use your Passport, not National ID.

**Q: When can I start teaching?**  
A: After admin reviews and approves your documents (status changes to Active).

**Q: Can I update my information after registration?**  
A: Contact admin for profile updates. Document updates require admin approval.

---

## Support

For technical issues or questions:

- Email: <support@qalam.com>
- Developer Portal: <https://developers.qalam.com>

---

## Changelog

### Version 1.0.0 (January 2026)

- Initial release
- 4-step phone-based registration
- Automatic user identity injection via pipeline behavior
- Document upload with validation
- Saudi Arabia identity rules
- International teacher support
