# API Testing Guide - Complete Multi-Domain Testing

This guide provides step-by-step API requests to test the complete flow across all domains: School, Quran, Languages, and Skills.

## Prerequisites

- API running at `http://localhost:5000` (or your configured URL)
- Use Postman, Swagger UI, or similar API testing tool
- Save tokens from authentication responses for subsequent requests

---

## Phase 1: Teacher Registration & Setup

### Step 1.1: Teacher Registration - Send OTP

**Endpoint:** `POST /Api/V1/Authentication/Teacher/LoginOrRegister`

**Request Body:**

```json
{
  "countryCode": "+966",
  "phoneNumber": "501234567"
}
```

**Response:**

```json
{
  "succeeded": true,
  "data": {
    "isNewUser": true,
    "message": "OTP sent successfully"
  }
}
```

### Step 1.2: Teacher - Verify OTP

**Endpoint:** `POST /Api/V1/Authentication/Teacher/VerifyOtp`

**Request Body:**

```json
{
  "phoneNumber": "+966501234567",
  "otpCode": "123456"
}
```

**Response:**

```json
{
  "succeeded": true,
  "data": {
    "token": "eyJhbGc...",
    "isNewUser": true,
    "nextStep": "CompletePersonalInfo"
  }
}
```

**Save the token!** Use it in all subsequent Teacher requests as: `Authorization: Bearer eyJhbGc...`

### Step 1.3: Teacher - Complete Personal Info

**Endpoint:** `POST /Api/V1/Authentication/Teacher/CompletePersonalInfo`

**Headers:** `Authorization: Bearer <teacher_token>`

**Request Body:**

```json
{
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "email": "ahmed.teacher@example.com",
  "password": "SecurePass123!",
  "dateOfBirth": "1990-05-15",
  "gender": 1,
  "identityType": 1,
  "identityNumber": "1234567890",
  "cityOrRegion": "Riyadh"
}
```

---

## Phase 2: Teacher Adds Subjects in All Domains

Before adding subjects, you need to know the subject IDs for each domain. Reference IDs from seeded data:

**Domain IDs:**

- 1 = School Education
- 2 = Quran
- 3 = Languages
- 4 = General Skills
- 5 = University

### Step 2.1: Add School Subjects

**Endpoint:** `POST /Api/V1/Teacher/TeacherSubject`

**Headers:** `Authorization: Bearer <teacher_token>`

**Request Body:**

```json
{
  "subjects": [
    {
      "subjectId": 1,
      "canTeachFullSubject": false,
      "curriculumId": 1,
      "levelId": 3,
      "gradeId": 10,
      "units": [1, 2, 3]
    },
    {
      "subjectId": 2,
      "canTeachFullSubject": true,
      "curriculumId": 1,
      "levelId": 2,
      "gradeId": 7,
      "units": []
    }
  ]
}
```

**Notes:**

- Subject IDs: Get from `GET /Api/V1/Subjects/Domain/1` (School domain)
- CurriculumId: 1 = Saudi Curriculum
- LevelId: 1=Elementary, 2=Middle School, 3=High School
- GradeId: Varies by level

### Step 2.2: Add Quran Subject

**Request Body:**

```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "quranContentTypeId": 1,
      "quranLevelId": 2,
      "units": [1, 2, 3, 4, 5]
    }
  ]
}
```

**Notes:**

- SubjectId: 499 = Holy Quran (from seeded data)
- QuranContentTypeId: 1=Memorization, 2=Recitation, 3=Tajweed
- QuranLevelId: 1=Noorani, 2=Beginner, 3=Intermediate, 4=Advanced
- Units: Quran Parts or Surahs (check ContentUnits)
- No CurriculumId, LevelId, or GradeId for Quran

### Step 2.3: Add Language Subject

**Request Body:**

```json
{
  "subjects": [
    {
      "subjectId": 600,
      "canTeachFullSubject": true,
      "levelId": 1,
      "units": []
    }
  ]
}
```

**Notes:**

- SubjectId: Get from `GET /Api/V1/Subjects/Domain/3` (Languages domain)
- LevelId: 1=Beginner, 2=Intermediate, 3=Advanced
- No CurriculumId or GradeId for Languages

### Step 2.4: Add Skills Subject

**Request Body:**

```json
{
  "subjects": [
    {
      "subjectId": 700,
      "canTeachFullSubject": true,
      "units": []
    }
  ]
}
```

**Notes:**

- SubjectId: Get from `GET /Api/V1/Subjects/Domain/4` (Skills domain)
- No Curriculum, Level, or Grade for Skills domain

---

## Phase 3: Teacher Creates Courses

After adding subjects via `POST /Api/V1/Teacher/TeacherSubject`, get your `TeacherSubjectId` values:

**Get Teacher Subjects:** `GET /Api/V1/Teacher/TeacherSubject`

### Step 3.1: Create School Course

**Endpoint:** `POST /Api/V1/Teacher/TeacherCourse`

**Headers:** `Authorization: Bearer <teacher_token>`

**Request Body:**

```json
{
  "title": "Arabic Language - Grade 10",
  "description": "Complete Arabic language curriculum for Grade 10",
  "teacherSubjectId": 1,
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "isFlexible": false,
  "sessionsCount": 20,
  "sessionDurationMinutes": 60,
  "price": 500,
  "maxStudents": 10,
  "canIncludeInPackages": true
}
```

**Notes:**

- teacherSubjectId: From Step 2.1 (your saved school subject)
- teachingModeId: 1=In-Person, 2=Online
- sessionTypeId: 1=Individual, 2=Group

### Step 3.2: Create Quran Course

**Request Body:**

```json
{
  "title": "Quran Memorization - Beginner",
  "description": "Memorizing Juz Amma with Tajweed rules",
  "teacherSubjectId": 2,
  "teachingModeId": 2,
  "sessionTypeId": 1,
  "isFlexible": true,
  "sessionsCount": null,
  "sessionDurationMinutes": 45,
  "price": 300,
  "maxStudents": 5,
  "canIncludeInPackages": false
}
```

### Step 3.3: Create Language Course

**Request Body:**

```json
{
  "title": "English for Beginners",
  "description": "Basic English conversation and grammar",
  "teacherSubjectId": 3,
  "teachingModeId": 2,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionsCount": 15,
  "sessionDurationMinutes": 90,
  "price": 400,
  "maxStudents": 8,
  "canIncludeInPackages": true
}
```

### Step 3.4: Create Skills Course

**Request Body:**

```json
{
  "title": "Computer Programming Basics",
  "description": "Introduction to Python programming",
  "teacherSubjectId": 4,
  "teachingModeId": 2,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionsCount": 12,
  "sessionDurationMinutes": 120,
  "price": 600,
  "maxStudents": 15,
  "canIncludeInPackages": true
}
```

### Step 3.5: Publish Courses

For each course created, update its status to Published:

**Endpoint:** `PUT /Api/V1/Teacher/TeacherCourse/{courseId}`

**Request Body:**

```json
{
  "title": "Arabic Language - Grade 10",
  "description": "Complete Arabic language curriculum for Grade 10",
  "teacherSubjectId": 1,
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "isFlexible": false,
  "sessionsCount": 20,
  "sessionDurationMinutes": 60,
  "price": 500,
  "maxStudents": 10,
  "canIncludeInPackages": true,
  "status": 1
}
```

Note: Status 1 = Published (makes it visible in student catalog)

---

## Phase 4: Guardian Registration

### Step 4.1: Guardian - Send OTP

**Endpoint:** `POST /Api/V1/Authentication/Student/SendOtp`

**Request Body:**

```json
{
  "phoneNumber": "+966509876543"
}
```

### Step 4.2: Guardian - Verify OTP

**Endpoint:** `POST /Api/V1/Authentication/Student/VerifyOtp`

**Request Body:**

```json
{
  "phoneNumber": "+966509876543",
  "otpCode": "123456"
}
```

**Save the token!** Use as: `Authorization: Bearer <guardian_token>`

### Step 4.3: Guardian - Set Account Type

**Endpoint:** `POST /Api/V1/Authentication/Student/SetAccountTypeAndUsage`

**Headers:** `Authorization: Bearer <guardian_token>`

**Request Body:**

```json
{
  "accountType": "Both",
  "usageMode": "Both",
  "firstName": "Fatima",
  "lastName": "Ali",
  "email": "fatima.guardian@example.com",
  "password": "SecurePass123!",
  "dateOfBirth": "1985-08-20",
  "cityOrRegion": "Jeddah"
}
```

**Notes:**

- accountType: "Both" creates both Student AND Guardian records
- usageMode: "Both" means will study themselves AND manage children

### Step 4.4: Guardian - Complete Academic Profile (for self)

**Endpoint:** `POST /Api/V1/Authentication/Student/CompleteProfile`

**Headers:** `Authorization: Bearer <guardian_token>`

**Request Body:**

```json
{
  "domainId": 3,
  "levelId": 2
}
```

**Notes:**

- This is for the guardian's own student profile (if they want to learn)
- Language domain (3) + Intermediate level (2)

---

## Phase 5: Guardian Adds Children

### Step 5.1: Add Child for School Domain

**Endpoint:** `POST /Api/V1/Authentication/Student/AddChild`

**Headers:** `Authorization: Bearer <guardian_token>`

**Request Body:**

```json
{
  "child": {
    "fullName": "Sara Ali",
    "dateOfBirth": "2014-03-10",
    "gender": 2,
    "guardianRelation": 2,
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 3,
    "gradeId": 10
  }
}
```

**Notes:**

- School domain requires all 4 fields: domainId, curriculumId, levelId, gradeId
- guardianRelation: 1=Father, 2=Mother, 3=Brother, 4=Sister

### Step 5.2: Add Child for Quran Domain

**Request Body:**

```json
{
  "child": {
    "fullName": "Mohammed Ali",
    "dateOfBirth": "2012-07-25",
    "gender": 1,
    "guardianRelation": 1,
    "domainId": 2,
    "curriculumId": null,
    "levelId": null,
    "gradeId": null
  }
}
```

**Notes:**

- Quran domain: Only domainId needed, rest are null

### Step 5.3: Add Child for Languages Domain

**Request Body:**

```json
{
  "child": {
    "fullName": "Layla Ali",
    "dateOfBirth": "2015-11-05",
    "gender": 2,
    "guardianRelation": 2,
    "domainId": 3,
    "curriculumId": null,
    "levelId": 1,
    "gradeId": null
  }
}
```

**Notes:**

- Languages domain: domainId + levelId (Beginner), no curriculum or grade

---

## Phase 6: Search and Browse Courses

### Step 6.1: Guardian - Get Children List

**Endpoint:** `GET /Api/V1/Student/MyChildren`

**Headers:** `Authorization: Bearer <guardian_token>`

**Response:**

```json
{
  "succeeded": true,
  "data": [
    {
      "id": 1,
      "fullName": "Sara Ali",
      "dateOfBirth": "2014-03-10",
      "gender": 2,
      "guardianRelation": 2,
      "domainId": 1,
      "domainNameEn": "School Education",
      "curriculumId": 1,
      "curriculumNameEn": "Saudi Curriculum",
      "levelId": 3,
      "levelNameEn": "High School",
      "gradeId": 10,
      "gradeNameEn": "Grade 10",
      "isActive": true
    }
  ]
}
```

### Step 6.2: Browse Courses for School Child (Sara)

**Endpoint:** `GET /Api/V1/Student/Courses?StudentId=1&PageNumber=1&PageSize=10`

**Headers:** `Authorization: Bearer <guardian_token>`

**Query Parameters:**

- `StudentId=1` (Sara's ID from Step 6.1)
- Automatically filters by Sara's profile: Domain=1, Curriculum=1, Level=3, Grade=10

**Expected:** Shows only School courses matching Grade 10

### Step 6.3: Browse Courses for Quran Child (Mohammed)

**Endpoint:** `GET /Api/V1/Student/Courses?StudentId=2&PageNumber=1&PageSize=10`

**Headers:** `Authorization: Bearer <guardian_token>`

**Expected:** Shows only Quran courses (no level/grade filtering)

### Step 6.4: Browse Courses for Language Child (Layla)

**Endpoint:** `GET /Api/V1/Student/Courses?StudentId=3&PageNumber=1&PageSize=10`

**Headers:** `Authorization: Bearer <guardian_token>`

**Expected:** Shows only Language courses, Level 1 (Beginner)

### Step 6.5: Guardian Browse Courses for Self

**Endpoint:** `GET /Api/V1/Student/Courses?PageNumber=1&PageSize=10`

**Headers:** `Authorization: Bearer <guardian_token>`

**Note:** No StudentId = uses guardian's own student profile (Language - Intermediate)

**Expected:** Shows Language courses, Level 2 (Intermediate)

### Step 6.6: Override Filters

**Endpoint:** `GET /Api/V1/Student/Courses?StudentId=1&DomainId=2&PageNumber=1&PageSize=10`

**Expected:** Shows Quran courses instead of School (override child's domain)

---

## Phase 7: Enroll in Course

### Step 7.1: Request Enrollment

**Endpoint:** `POST /Api/V1/Student/EnrollmentRequests`

**Headers:** `Authorization: Bearer <guardian_token>`

**Request Body:**

```json
{
  "courseId": 1,
  "teachingModeId": 1,
  "notes": "Preferred time: Evening"
}
```

### Step 7.2: View My Enrollment Requests

**Endpoint:** `GET /Api/V1/Student/EnrollmentRequests`

**Headers:** `Authorization: Bearer <guardian_token>`

---

## Quick Reference: Important IDs

### Domains:

- 1 = School Education (needs Curriculum + Level + Grade)
- 2 = Quran (no Curriculum/Level/Grade)
- 3 = Languages (needs Level only)
- 4 = Skills (no additional fields)
- 5 = University (needs Level)

### Teaching Modes:

- 1 = In-Person
- 2 = Online

### Session Types:

- 1 = Individual
- 2 = Group

### Guardian Relations:

- 1 = Father
- 2 = Mother
- 3 = Brother
- 4 = Sister
- 5 = Uncle
- 6 = Aunt

### Course Status:

- 0 = Draft
- 1 = Published
- 2 = InProgress
- 3 = Completed
- 4 = Cancelled

---

## Testing Checklist

- [ ] Teacher registers and completes profile
- [ ] Teacher adds subjects in School domain
- [ ] Teacher adds subjects in Quran domain
- [ ] Teacher adds subjects in Languages domain
- [ ] Teacher adds subjects in Skills domain
- [ ] Teacher creates courses for each domain
- [ ] Teacher publishes all courses
- [ ] Guardian registers (AccountType = Both)
- [ ] Guardian completes own profile
- [ ] Guardian adds 3+ children with different domains
- [ ] Guardian views children list
- [ ] Guardian browses courses for each child (filtered correctly)
- [ ] Guardian browses courses for self
- [ ] Student/Guardian requests enrollment
- [ ] Verify each domain filters correctly (School shows grades, Quran doesn't, etc.)

---

## Expected Behavior by Domain

| Domain     | DomainId | CurriculumId | LevelId  | GradeId  |
| ---------- | -------- | ------------ | -------- | -------- |
| School     | Required | Required     | Required | Required |
| Quran      | Required | N/A          | N/A      | N/A      |
| Languages  | Required | N/A          | Required | N/A      |
| Skills     | Required | N/A          | N/A      | N/A      |
| University | Required | N/A          | Required | N/A      |

This validates that your nullable design (`int?`) for CurriculumId/LevelId/GradeId is correct!

---

# Database Cleanup for Testing

## SQL Server - Clear Test Data (Preserving Roles and Admin)

Use these queries to reset your database for testing without losing seed data, roles, or the admin user.

```sql
-- =============================================
-- Clear Test Data (SQL Server)
-- Preserves: Roles, Admin User (admin@qalam.com)
-- =============================================

BEGIN TRANSACTION;

DECLARE @AdminUserId NVARCHAR(450);
SELECT @AdminUserId = Id FROM security.Users WHERE Email = 'admin@qalam.com';

-- 1. Clear course-related data (course schema)
DELETE FROM course.CourseEnrollments;
DELETE FROM course.CourseEnrollmentRequests;
DELETE FROM course.CourseSessions;
DELETE FROM course.CourseSchedules;
DELETE FROM course.Courses;

-- 2. Clear teacher-subject relationships
DELETE FROM teacher.TeacherSubjectUnits;
DELETE FROM education.TeacherSubjects;

-- 3. Clear teacher-related tables (dbo schema - default)
DELETE FROM dbo.TeacherAvailabilityExceptions;
DELETE FROM dbo.TeacherAvailabilities;
DELETE FROM dbo.TeacherAreas;
DELETE FROM dbo.TeacherReviews;
DELETE FROM dbo.TeacherDocuments;
DELETE FROM dbo.TeacherAuditLogs;
DELETE FROM dbo.Teachers;

-- 4. Clear guardian and student data (student schema)
DELETE FROM student.Students WHERE GuardianId IS NOT NULL; -- Children first
DELETE FROM student.Guardians;
DELETE FROM student.Students; -- Remaining students

-- 5. Clear users (except admin) and their roles
DELETE FROM security.UserRoles WHERE UserId != @AdminUserId;
DELETE FROM security.Users WHERE Email != 'admin@qalam.com';

-- 6. Reset identity seeds
DBCC CHECKIDENT ('course.Courses', RESEED, 0);
DBCC CHECKIDENT ('course.CourseEnrollments', RESEED, 0);
DBCC CHECKIDENT ('course.CourseEnrollmentRequests', RESEED, 0);
DBCC CHECKIDENT ('education.TeacherSubjects', RESEED, 0);
DBCC CHECKIDENT ('teacher.TeacherSubjectUnits', RESEED, 0);
DBCC CHECKIDENT ('dbo.Teachers', RESEED, 0);
DBCC CHECKIDENT ('student.Guardians', RESEED, 0);
DBCC CHECKIDENT ('student.Students', RESEED, 0);

COMMIT TRANSACTION;

-- Verify preserved data
SELECT 'Roles' AS TableName, COUNT(*) AS Count FROM security.Roles
UNION ALL
SELECT 'Admin User', COUNT(*) FROM security.Users WHERE Email = 'admin@qalam.com'
UNION ALL
SELECT 'All Users', COUNT(*) FROM security.Users;
```

### Schema Mappings

| Schema | Tables |
|--------|--------|
| **security** | Users, Roles, UserRoles (Identity tables) |
| **student** | Students, Guardians |
| **education** | TeacherSubjects, EducationDomains, Subjects, Curriculums, Grades, etc. |
| **teacher** | TeacherSubjectUnits |
| **dbo** (default) | Teachers, TeacherDocuments, TeacherAreas, TeacherReviews, TeacherAuditLogs, TeacherAvailabilities, TeacherAvailabilityExceptions |
| **course** | Courses, CourseEnrollments, CourseEnrollmentRequests, CourseSessions, CourseSchedules |
| **quran** | QuranLevels, QuranSurahs, QuranParts, QuranContentTypes |
| **teaching** | TeachingModes, SessionTypes, EducationRules, DomainTeachingModes |
| **common** | Locations, TimeSlots, SystemSettings |

### What is Preserved

- All seed data (Domains, Subjects, Curriculums, Grades, etc.)
- All Identity Roles (SuperAdmin, Admin, Staff, Teacher, Student, Guardian)
- Admin user account (admin@qalam.com)

### What is Deleted

- All test users (teachers, guardians, students)
- All teacher subjects and units
- All courses and enrollments
- All user role assignments (except admin)

---

## Testing Workflow

1. **Reset Database:** Run the SQL cleanup query above
2. **Verify Clean State:** Check that only admin user and roles remain
3. **Run API Tests:** Follow the API Testing Guide from Phase 1
4. **Repeat:** After each test cycle, reset database and start fresh

---

## Notes

- The cleanup script preserves all reference/seed data in `education`, `quran`, `teaching`, and `common` schemas
- Identity seeds are reset to 0 for a clean start
- The script uses a transaction, so if any step fails, all changes are rolled back
- Always backup your database before running cleanup scripts in non-development environments
