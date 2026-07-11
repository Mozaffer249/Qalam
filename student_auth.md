# Student Authentication - All Cases & Scenarios

Base URL: `Api/V1/Authentication/Student`

---

## 1. SendOtp

**`POST /SendOtp`** — No auth

### Request
```json
{
  "countryCode": "+966",
  "phoneNumber": "503788444"
}
```

### Case 1.1: New phone number
```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "isNewUser": true,
    "message": "OTP sent successfully. Complete registration after verification.",
    "phoneNumber": "*******8444"
  }
}
```

### Case 1.2: Existing user WITH Student/Guardian role
```json
{
  "data": {
    "isNewUser": false,
    "message": "OTP sent successfully. Sign in after verification.",
    "phoneNumber": "*******8444"
  }
}
```

### Case 1.3: Existing user WITHOUT Student/Guardian role (e.g. Teacher)
```json
{
  "data": {
    "isNewUser": false,
    "message": "OTP sent successfully. Complete registration after verification.",
    "phoneNumber": "*******8444"
  }
}
```

### Error: Invalid phone format
```json
{
  "statusCode": "UnprocessableEntity",
  "succeeded": false,
  "message": "Validation errors",
  "errors": ["'Phone Number' is not in the correct format."]
}
```

### Error: Invalid country code
```json
{
  "errors": ["'Country Code' is not in the correct format."]
}
```

---

## 2. VerifyOtp

**`POST /VerifyOtp`** — No auth

### Request
```json
{
  "phoneNumber": "503788444",
  "otpCode": "1234"
}
```
> Test OTP code: `"1234"` always passes (assumes +966 country code)

### Case 2.1: New user (no account yet)
**Next:** ChooseAccountType screen
```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "currentStep": 1,
    "nextStepName": "ChooseAccountType",
    "isNextStepRequired": true,
    "optionalSteps": [],
    "nextStepDescription": "Choose your account type and complete profile.",
    "isRegistrationComplete": false,
    "message": "Verified. Choose account type and complete profile."
  }
}
```

### Case 2.2: Existing user without Student/Guardian role
**Next:** ChooseAccountType screen (to add student/parent capabilities)
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "currentStep": 1,
    "nextStepName": "ChooseAccountType",
    "isNextStepRequired": true,
    "optionalSteps": [],
    "nextStepDescription": "Choose account type to add student/parent capabilities.",
    "isRegistrationComplete": false,
    "message": "Verified. Choose account type to add student/parent capabilities."
  }
}
```

### Case 2.3: Existing user WITH Student/Guardian role (login)
**Next:** Dashboard (skip registration)
```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "currentStep": 1,
    "nextStepName": "Dashboard",
    "isNextStepRequired": false,
    "optionalSteps": [],
    "nextStepDescription": "Welcome back!",
    "isRegistrationComplete": true,
    "message": "Signed in successfully."
  }
}
```

### Error: Wrong OTP code
```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "Invalid or expired OTP code",
  "data": null
}
```

---

## 3. SetAccountTypeAndUsage

**`POST /SetAccountTypeAndUsage`** — Requires `Bearer <token>` from VerifyOtp

> **Important:** Response returns a NEW token with updated roles. Replace stored token.

### Case 3.1: accountType = "Student"
**Roles assigned:** Student
**Next:** CompleteAcademicProfile (required)

```json
// REQUEST
{
  "data": {
    "accountType": "Student",
    "firstName": "Ahmed",
    "lastName": "Ali",
    "email": "ahmed@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "2000-01-15"
  }
}
```
```json
// RESPONSE
{
  "data": {
    "token": "eyJ...(includes role:Student)...",
    "currentStep": 2,
    "nextStepName": "CompleteAcademicProfile",
    "isNextStepRequired": true,
    "optionalSteps": [],
    "nextStepDescription": "Complete your academic profile to start.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  }
}
```

### Case 3.2: accountType = "Parent", usageMode = "StudySelf"
**Roles assigned:** Guardian
**Next:** CompleteAcademicProfile (required), then optionally AddChildren

```json
// REQUEST
{
  "data": {
    "accountType": "Parent",
    "usageMode": "StudySelf",
    "firstName": "Fatima",
    "lastName": "Hassan",
    "email": "fatima@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1985-03-20",
    "cityOrRegion": "Riyadh"
  }
}
```
```json
// RESPONSE
{
  "data": {
    "token": "eyJ...(includes role:Guardian)...",
    "currentStep": 2,
    "nextStepName": "CompleteAcademicProfile",
    "isNextStepRequired": true,
    "optionalSteps": ["AddChildren"],
    "nextStepDescription": "Complete your academic profile. You can also add children later.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  }
}
```

### Case 3.3: accountType = "Parent", usageMode = "AddChildren"
**Roles assigned:** Guardian
**Next:** AddChildren (optional — can skip to Dashboard)

```json
// REQUEST
{
  "data": {
    "accountType": "Parent",
    "usageMode": "AddChildren",
    "firstName": "Khalid",
    "lastName": "Saeed",
    "email": "khalid@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1988-11-05"
  }
}
```
```json
// RESPONSE
{
  "data": {
    "token": "eyJ...(includes role:Guardian)...",
    "currentStep": 2,
    "nextStepName": "AddChildren",
    "isNextStepRequired": false,
    "optionalSteps": ["Dashboard"],
    "nextStepDescription": "You can add children now or skip to dashboard.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  }
}
```

### Case 3.4: accountType = "Parent", usageMode = "Both"
**Roles assigned:** Guardian
**Next:** CompleteAcademicProfile (required), then optionally AddChildren

```json
// REQUEST
{
  "data": {
    "accountType": "Parent",
    "usageMode": "Both",
    "firstName": "Nora",
    "lastName": "Ahmed",
    "email": "nora@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1992-06-18"
  }
}
```
```json
// RESPONSE
{
  "data": {
    "token": "eyJ...(includes role:Guardian)...",
    "currentStep": 2,
    "nextStepName": "CompleteAcademicProfile",
    "isNextStepRequired": true,
    "optionalSteps": ["AddChildren"],
    "nextStepDescription": "Complete your academic profile first, then you can add children.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  }
}
```

### Case 3.5: accountType = "Both"
**Roles assigned:** Student + Guardian
**Next:** CompleteAcademicProfile (required), then optionally AddChildren

```json
// REQUEST
{
  "data": {
    "accountType": "Both",
    "usageMode": "Both",
    "firstName": "Mohammed",
    "lastName": "Ibrahim",
    "email": "mohammed@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1990-07-10"
  }
}
```
```json
// RESPONSE
{
  "data": {
    "token": "eyJ...(includes role:[Student,Guardian])...",
    "currentStep": 2,
    "nextStepName": "CompleteAcademicProfile",
    "isNextStepRequired": true,
    "optionalSteps": ["AddChildren"],
    "nextStepDescription": "Complete your academic profile. You can add children anytime.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  }
}
```

### Case 3.6: User already has the requested roles
**Next:** Dashboard (already set up)

```json
{
  "data": {
    "token": "eyJ...",
    "currentStep": 1,
    "nextStepName": "Dashboard",
    "isNextStepRequired": false,
    "optionalSteps": [],
    "nextStepDescription": "You're all set!",
    "isRegistrationComplete": true,
    "message": "Account already set up with requested roles."
  }
}
```

### Error: Under 18
```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "You must be 18 years or older to register.",
  "data": null
}
```

### Error: Email already taken
```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "Email is already registered.",
  "data": null
}
```

### Error: Missing usageMode for Parent/Both
```json
{
  "statusCode": "UnprocessableEntity",
  "succeeded": false,
  "errors": ["UsageMode is required when AccountType is 'Parent' or 'Both'."]
}
```

### Error: User not found (invalid token)
```json
{
  "statusCode": "NotFound",
  "succeeded": false,
  "message": "User not found."
}
```

---

## 4. CompleteProfile

**`POST /CompleteProfile`** — Requires `Bearer <token>` (with Student or Guardian role)

> **Note:** Response `token` is `null`. Keep using the token from SetAccountTypeAndUsage.

### Request
```json
{
  "profile": {
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 1,
    "gradeId": 2
  }
}
```

### Case 4.1: Student only (no Guardian role)
**Next:** Dashboard

```json
{
  "data": {
    "token": null,
    "currentStep": 3,
    "nextStepName": "Dashboard",
    "isNextStepRequired": false,
    "optionalSteps": [],
    "nextStepDescription": "Profile completed successfully!",
    "isRegistrationComplete": true,
    "message": "Academic profile saved successfully."
  }
}
```

### Case 4.2: Student + Guardian (Both or Parent+StudySelf)
**Next:** Dashboard, with option to AddChildren

```json
{
  "data": {
    "token": null,
    "currentStep": 3,
    "nextStepName": "Dashboard",
    "isNextStepRequired": false,
    "optionalSteps": ["AddChildren"],
    "nextStepDescription": "Profile completed! You can add children or go to dashboard.",
    "isRegistrationComplete": true,
    "message": "Academic profile saved successfully."
  }
}
```

### Error: No student profile
```json
{
  "statusCode": "NotFound",
  "succeeded": false,
  "message": "Student profile not found. Complete registration first."
}
```

---

## 5. AddChild

**`POST /AddChild`** — Requires `Bearer <token>` with Guardian role

Can be called multiple times to add multiple children.

### Request
```json
{
  "child": {
    "fullName": "Ali Mohammed",
    "email": "ali@example.com",
    "password": "Abc@1234",
    "confirmPassword": "Abc@1234",
    "dateOfBirth": "2015-03-13",
    "gender": "Male",
    "guardianRelation": "Father",
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 1,
    "gradeId": 2
  }
}
```

### Case 5.1: Success
```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Child added successfully.",
  "data": 42
}
```
`data` = new child's student ID (integer).

### Error: Guardian not found
```json
{
  "statusCode": "NotFound",
  "message": "Guardian profile not found. Only parents can add children."
}
```

### Error: Email already registered
```json
{
  "statusCode": "BadRequest",
  "message": "Email is already registered."
}
```

### Error: Domain not found
```json
{ "statusCode": "BadRequest", "message": "Domain not found." }
```

### Error: Curriculum not found
```json
{ "statusCode": "BadRequest", "message": "Curriculum not found." }
```

### Error: Level not found
```json
{ "statusCode": "BadRequest", "message": "Level not found." }
```

### Error: Grade not found
```json
{ "statusCode": "BadRequest", "message": "Grade not found." }
```

### Error: Weak password (validation)
```json
{
  "statusCode": "UnprocessableEntity",
  "errors": [
    "Password must contain at least one uppercase letter.",
    "Password must contain at least one digit.",
    "Password must contain at least one special character."
  ]
}
```

### Error: Child over 18
```json
{
  "errors": ["Child must be under 18 years old."]
}
```

### Error: Password mismatch
```json
{
  "errors": ["ConfirmPassword must match Password."]
}
```

---

## Flow Summary Table

| Step | Endpoint | Auth | Next Step Logic |
|------|----------|------|-----------------|
| 1 | SendOtp | None | Always → VerifyOtp |
| 2 | VerifyOtp | None | Has Student/Guardian role → **Dashboard** / Otherwise → **ChooseAccountType** |
| 3 | SetAccountTypeAndUsage | Bearer | See routing table below |
| 4 | CompleteProfile | Bearer + Role | Always → **Dashboard** (+ optional AddChildren if Guardian) |
| 5 | AddChild | Bearer + Guardian | Repeatable. When done → **Dashboard** |

### SetAccountTypeAndUsage Routing Table

| accountType | usageMode | nextStepName | required? | optionalSteps |
|-------------|-----------|--------------|-----------|---------------|
| Student | — | CompleteAcademicProfile | YES | — |
| Parent | StudySelf | CompleteAcademicProfile | YES | AddChildren |
| Parent | AddChildren | AddChildren | NO | Dashboard |
| Parent | Both | CompleteAcademicProfile | YES | AddChildren |
| Both | Both | CompleteAcademicProfile | YES | AddChildren |

---

## Token Lifecycle

```
VerifyOtp          → token v1 (minimal claims, no roles)
SetAccountType     → token v2 (includes roles: Student, Guardian, or both)
CompleteProfile    → token = null (keep using v2)
AddChild           → no token change (keep using v2)
```

Always check: if `response.data.token !== null` → replace stored token.

---

## Entities Created Per Step

| Step | Entities Created |
|------|-----------------|
| SendOtp | None |
| VerifyOtp (new user) | User (phone only, temp password) |
| VerifyOtp (existing) | None |
| SetAccountType (Student) | Student profile + Student role |
| SetAccountType (Parent) | Guardian profile + Guardian role |
| SetAccountType (Both) | Student + Guardian profiles + both roles |
| CompleteProfile | Updates existing Student (domain/curriculum/level/grade) |
| AddChild | User (child account) + Student (linked to Guardian) |

---

## Validation Rules Summary

### SendOtp
- `countryCode`: required, regex `^\+[0-9]{1,4}$`
- `phoneNumber`: required, regex `^[0-9]{9,15}$`

### VerifyOtp
- `phoneNumber`: required
- `otpCode`: required

### SetAccountTypeAndUsage
- `accountType`: required, one of "Student" / "Parent" / "Both"
- `usageMode`: required if Parent or Both, one of "StudySelf" / "AddChildren" / "Both"
- `firstName`: required
- `lastName`: required
- `email`: required, valid email, unique
- `password`: required, min 6 chars
- `dateOfBirth`: required, "YYYY-MM-DD", must be 18+

### CompleteProfile
- `domainId`: required, > 0
- `curriculumId`: optional, > 0
- `levelId`: optional, > 0
- `gradeId`: optional, > 0

### AddChild
- `fullName`: required, max 100
- `email`: required, valid email, unique
- `password`: required, min 6 + uppercase + lowercase + digit + special char
- `confirmPassword`: required, must match password
- `dateOfBirth`: required, under 18, not future
- `gender`: optional, "Male" or "Female"
- `guardianRelation`: optional, "Father"/"Mother"/"Brother"/"Sister"/"Uncle"/"Aunt"/"Grandfather"/"Grandmother"/"Other"
- `domainId`: optional, > 0
- `curriculumId`: optional, requires domainId
- `levelId`: optional, requires curriculumId
- `gradeId`: optional, requires levelId