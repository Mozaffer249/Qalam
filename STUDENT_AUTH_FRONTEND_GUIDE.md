# Student Authentication - Frontend Implementation Guide

## Overview

Multi-step registration flow: Phone OTP -> Account Type -> Academic Profile -> Dashboard.
Supports 3 account types: Student, Parent, Both.

**Base URL:** `Api/V1/Authentication/Student/`

All responses are wrapped in:

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": { ... },
  "errors": null,
  "meta": null
}
```

---

## Registration Flow Diagram

```
Screen 1: Phone Input
    POST /SendOttp
        |
Screen 2: OTP Verification
    POST /VerifyOp → returns token
        |
        ├── Existing user with Student/Guardian role → Dashboard
        |
Screen 3-4: Account Type + Personal Info
    POST /SetAccountTypeAndUsage  (Bearer token required)
        |                          → returns NEW token (with roles)
        ├── Student → CompleteAcademicProfile (required)
        ├── Parent + StudySelf → CompleteAcademicProfile (required) + AddChildren (optional)
        ├── Parent + AddChildren → AddChildren (optional) or Dashboard
        ├── Parent + Both → CompleteAcademicProfile (required) + AddChildren (optional)
        └── Both → CompleteAcademicProfile (required) + AddChildren (optional)
        |
Screen 5: Academic Profile
    POST /CompleteProfile  (Bearer token required)
        |                   → token is null (keep using previous token)
        └── Dashboard (optionally AddChildren if Guardian)
        |
Screen 6 (optional): Add Child
    POST /AddChild  (Bearer token + Guardian role required)
        |             → can repeat multiple times
        └── Dashboard
```

**Important: Token updates at each step.** When a response contains a non-null `token`, replace the stored token. The new token may contain updated claims (e.g., roles added after SetAccountTypeAndUsage).

---

## Step 1: Send OTP

**`POST /Api/V1/Authentication/Student/SendOttp`**

No auth required.

### Request

```typescript
interface SendOtpRequest {
  countryCode: string;  // e.g. "+966" — regex: ^\+[0-9]{1,4}$
  phoneNumber: string;  // e.g. "501234567" — regex: ^[0-9]{9,15}$
}
```

### Response

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "isNewUser": true,
    "message": "OTP sent successfully. Complete registration after verification.",
    "phoneNumber": "*******4567"
  },
  "errors": null,
  "meta": null
}
```

```typescript
interface SendOtpResponseData {
  isNewUser: boolean;       // true = registration path, false = login path
  message: string;
  phoneNumber: string;      // masked: "*******4567"
}
```

### Frontend Logic

- Show phone input with country code dropdown (default "+966")
- On success: navigate to OTP screen, store `isNewUser` to show "Register" vs "Sign In" context
- On error: show validation message

---

## Step 2: Verify OTP

**`POST /Api/V1/Authentication/Student/VerifyOp`**

No auth required.

### Request

```typescript
interface VerifyOtpRequest {
  phoneNumber: string;  // same phone from step 1 (without country code)
  otpCode: string;      // OTP code (test code: "1234")
}
```

### Response Sample (New User)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "currentStep": 1,
    "nextStepName": "ChooseAccountType",
    "isNextStepRequired": true,
    "optionalSteps": [],
    "nextStepDescription": "Choose account type to add student/parent capabilities.",
    "isRegistrationComplete": false,
    "message": "Verified. Choose account type to add student/parent capabilities."
  },
  "errors": null,
  "meta": null
}
```

### Response Sample (Existing User)

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

```typescript
interface RegistrationResponseData {
  token: string | null;         // JWT — store this, use as Bearer for subsequent calls
  currentStep: number;          // current step number
  nextStepName: string;         // "ChooseAccountType" | "Dashboard" | "CompleteAcademicProfile" | "AddChildren"
  isNextStepRequired: boolean;  // true = user MUST complete this step
  optionalSteps: string[];      // additional optional actions available
  nextStepDescription: string;  // user-friendly guidance text
  isRegistrationComplete: boolean;
  message: string;
}
```

### Frontend Logic

```typescript
const { data } = response.data;

// Always store token when present
if (data.token) {
  localStorage.setItem('token', data.token);
}

// Route based on nextStepName
switch (data.nextStepName) {
  case 'Dashboard':
    navigate('/dashboard');
    break;
  case 'ChooseAccountType':
    navigate('/register/account-type');
    break;
  case 'CompleteAcademicProfile':
    navigate('/register/academic-profile');
    break;
  case 'AddChildren':
    navigate('/register/add-child');
    break;
}
```

---

## Step 3-4: Set Account Type & Usage

**`POST /Api/V1/Authentication/Student/SetAccountTypeAndUsage`**

Requires: `Authorization: Bearer <token>` (from VerifyOtp)

### Request

```typescript
interface SetAccountTypeRequest {
  data: {
    accountType: "Student" | "Parent" | "Both";
    usageMode?: "StudySelf" | "AddChildren" | "Both";  // required if accountType is "Parent" or "Both"
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    dateOfBirth: string;     // "YYYY-MM-DD"
    cityOrRegion?: string;
  }
}
```

### Request Sample

```json
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

### Response Sample

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "currentStep": 2,
    "nextStepName": "CompleteAcademicProfile",
    "isNextStepRequired": true,
    "optionalSteps": [
      "AddChildren"
    ],
    "nextStepDescription": "Complete your academic profile. You can add children anytime.",
    "isRegistrationComplete": false,
    "message": "Account type set successfully."
  },
  "errors": null,
  "meta": null
}
```

**Important:** This response returns a **NEW token** with updated claims including the user's roles (`Student`, `Guardian`, or both). You MUST replace the stored token with this new one.

### Validation Rules

| Field | Rule |
|-------|------|
| `accountType` | Required. "Student", "Parent", or "Both" (case-insensitive) |
| `usageMode` | Required when accountType is "Parent" or "Both". Ignored for "Student" |
| `firstName` | Required, not empty |
| `lastName` | Required, not empty |
| `email` | Required, valid email, must be unique in the system |
| `password` | Required, minimum 6 characters |
| `dateOfBirth` | Required, format "YYYY-MM-DD", user must be 18 years or older |

### Routing Table

| accountType | usageMode | nextStepName | isNextStepRequired | optionalSteps |
|-------------|-----------|--------------|-------------------|---------------|
| Student | -- | CompleteAcademicProfile | true | [] |
| Parent | StudySelf | CompleteAcademicProfile | true | ["AddChildren"] |
| Parent | AddChildren | AddChildren | false | ["Dashboard"] |
| Parent | Both | CompleteAcademicProfile | true | ["AddChildren"] |
| Both | Both | CompleteAcademicProfile | true | ["AddChildren"] |

### Frontend Logic

```
Screen 3: Account Type Selection (radio/card selection)
  - "Student"  → "I want to learn"
  - "Parent"   → "I want to manage my children's education"
  - "Both"     → "I want to learn AND manage my children"

If Parent or Both selected → show UsageMode selector:
  - "StudySelf"    → "I will study too"
  - "AddChildren"  → "I will only add children"
  - "Both"         → "I will study AND add children"

Screen 4: Personal Info Form
  - firstName, lastName, email, password, dateOfBirth, cityOrRegion(optional)

On success:
  1. REPLACE stored token with response.data.token
  2. Navigate based on nextStepName
  3. If optionalSteps includes "AddChildren", show skip-able "Add Children" option
```

---

## Step 5: Complete Academic Profile

**`POST /Api/V1/Authentication/Student/CompleteProfile`**

Requires: `Authorization: Bearer <token>` (from SetAccountTypeAndUsage)

### Request

```typescript
interface CompleteProfileRequest {
  profile: {
    domainId: number;        // required, > 0
    curriculumId?: number;   // optional, > 0
    levelId?: number;        // optional, > 0
    gradeId?: number;        // optional, > 0
  }
}
```

### Request Sample

```json
{
  "profile": {
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 1
  }
}
```

### Response Sample

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Success",
  "data": {
    "token": null,
    "currentStep": 3,
    "nextStepName": "Dashboard",
    "isNextStepRequired": false,
    "optionalSteps": [
      "AddChildren"
    ],
    "nextStepDescription": "Profile completed! You can add children or go to dashboard.",
    "isRegistrationComplete": true,
    "message": "Academic profile saved successfully."
  },
  "errors": null,
  "meta": null
}
```

**Note:** `token` is `null` here -- keep using the token from the previous step.

### Dropdown Chain

Load each dropdown based on the parent selection:

```
Domain → Curriculum → Level → Grade
```

Endpoints to populate dropdowns:
- `GET /Api/V1/Education/Domains` — get all domains
- `GET /Api/V1/Curriculum?domainId=X` — get curricula for domain
- `GET /Api/V1/Education/Levels?curriculumId=X` — get levels for curriculum
- `GET /Api/V1/Education/Grades?levelId=X` — get grades for level

### Frontend Logic

```typescript
const { data } = response.data;

if (data.optionalSteps.includes('AddChildren')) {
  // Show two buttons:
  // [Add Children] → navigate('/register/add-child')
  // [Go to Dashboard] → navigate('/dashboard')
} else {
  navigate('/dashboard');
}
```

---

## Step 6: Add Child (Optional, Parent/Guardian only)

**`POST /Api/V1/Authentication/Student/AddChild`**

Requires: `Authorization: Bearer <token>`, Role: Guardian

### Request

```typescript
interface AddChildRequest {
  child: {
    fullName: string;                // required, max 100 chars
    email: string;                   // required, valid email, unique
    password: string;                // required, min 6 + uppercase + lowercase + digit + special
    confirmPassword: string;         // required, must match password
    dateOfBirth: string;             // "YYYY-MM-DD", must be under 18, not in future
    gender?: "Male" | "Female";      // optional, string
    guardianRelation?: string;       // optional: "Father","Mother","Brother","Sister","Uncle","Aunt","Grandfather","Grandmother","Other"
    domainId?: number;               // optional, > 0
    curriculumId?: number;           // optional, requires domainId
    levelId?: number;                // optional, requires curriculumId
    gradeId?: number;                // optional, requires levelId
  }
}
```

### Request Sample

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

### Response Sample (Success)

```json
{
  "statusCode": "OK",
  "succeeded": true,
  "message": "Child added successfully.",
  "data": 42,
  "errors": null,
  "meta": null
}
```

`data` is the new child's student ID (integer).

### Validation Rules

| Field | Rule |
|-------|------|
| `fullName` | Required, max 100 chars |
| `email` | Required, valid email, must be unique |
| `password` | Min 6 chars + at least: 1 uppercase [A-Z], 1 lowercase [a-z], 1 digit [0-9], 1 special char |
| `confirmPassword` | Must match `password` |
| `dateOfBirth` | Required, child must be under 18 years, not a future date |
| `gender` | Optional. String: "Male" or "Female" |
| `guardianRelation` | Optional. String: "Father", "Mother", "Brother", etc. |
| Academic fields | Hierarchical: gradeId needs levelId, levelId needs curriculumId, curriculumId needs domainId |

### Frontend Logic

- Show child registration form
- Password strength indicator (uppercase + lowercase + digit + special char)
- Same dropdown chain for academic profile (Domain -> Curriculum -> Level -> Grade)
- After success:
  - Show "Add Another Child" button (calls same endpoint again)
  - Show "Done / Go to Dashboard" button

---

## Error Handling

### Validation Errors (400)

```json
{
  "statusCode": "BadRequest",
  "succeeded": false,
  "message": "Validation errors occurred.",
  "data": null,
  "errors": [
    "Email is already registered.",
    "Password must be at least 6 characters."
  ]
}
```

Display each error from the `errors` array under the relevant field or as a toast.

### Unauthorized (401)

Token expired or invalid. Redirect to phone/OTP screen.

### Not Found (404)

```json
{
  "statusCode": "NotFound",
  "succeeded": false,
  "message": "Student profile not found.",
  "data": null,
  "errors": null
}
```

---

## Token Management

```typescript
// Axios interceptor example
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// After every auth response, update token if present
function handleAuthResponse(response: ApiResponse<RegistrationResponseData>) {
  const { data } = response.data;
  if (data.token) {
    localStorage.setItem('token', data.token);
  }
  return data;
}
```

**Token lifecycle:**
1. `VerifyOtp` returns initial token (minimal claims, no roles)
2. `SetAccountTypeAndUsage` returns NEW token (includes roles: Student, Guardian)
3. `CompleteProfile` returns `token: null` (keep using previous token)
4. `AddChild` returns child ID, no token change

---

## Navigation State Machine

```typescript
interface AuthState {
  token: string | null;
  currentStep: number;
  nextStepName: string;
  isNextStepRequired: boolean;
  optionalSteps: string[];
  isRegistrationComplete: boolean;
  phoneNumber: string;
}

// After every response, update state and navigate:
function handleStepResponse(data: RegistrationResponseData) {
  if (data.token) setToken(data.token);

  setAuthState({
    currentStep: data.currentStep,
    nextStepName: data.nextStepName,
    isNextStepRequired: data.isNextStepRequired,
    optionalSteps: data.optionalSteps,
    isRegistrationComplete: data.isRegistrationComplete,
  });

  navigateToStep(data.nextStepName);
}

function navigateToStep(step: string) {
  const routes: Record<string, string> = {
    'ChooseAccountType': '/register/account-type',
    'CompleteAcademicProfile': '/register/academic-profile',
    'AddChildren': '/register/add-child',
    'Dashboard': '/dashboard',
  };
  navigate(routes[step] || '/dashboard');
}
```

The server drives the flow -- always follow `nextStepName` rather than hardcoding step logic on the frontend.

---

## Enums Reference

### Gender (string values in request)
| String Value | Description |
|-------------|-------------|
| "Male" | Male |
| "Female" | Female |

### Guardian Relation (string values in request)
| String Value | Label AR |
|-------------|----------|
| "Father" | اب |
| "Mother" | ام |
| "Brother" | اخ |
| "Sister" | اخت |
| "Uncle" | عم |
| "Aunt" | عمة |
| "Grandfather" | جد |
| "Grandmother" | جدة |
| "Other" | اخرى |

### Account Type (string values in request)
| Value | Description |
|-------|-------------|
| "Student" | Can learn and enroll in courses |
| "Parent" | Can add and manage children |
| "Both" | Can learn AND manage children |

### Usage Mode (string values, for Parent/Both only)
| Value | Description |
|-------|-------------|
| "StudySelf" | Parent will study too |
| "AddChildren" | Parent only manages children |
| "Both" | Parent studies AND manages children |
