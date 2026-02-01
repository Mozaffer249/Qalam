# Teacher Subject API - Testing Guide

## Overview

This guide covers testing the Teacher Subject API endpoints for managing teacher subjects and units.

---

## Prerequisites

1. **Running API**: Ensure the API is running locally
2. **Teacher Account**: Register and login as a teacher to get JWT token
3. **Seeded Data**: Ensure subjects, units, QuranContentTypes, and QuranLevels are seeded

---

## Authentication

All endpoints require JWT token with `Teacher` role.

```
Authorization: Bearer <your_jwt_token>
```

---

## Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Api/V1/Teacher/TeacherSubject` | Get teacher's subjects |
| POST | `/Api/V1/Teacher/TeacherSubject` | Save teacher's subjects |

---

## Test Scenarios

### 1. Get Teacher Subjects (Empty)

**Request:**
```http
GET /Api/V1/Teacher/TeacherSubject
Authorization: Bearer <token>
```

**Expected Response (200 OK):**
```json
{
  "succeeded": true,
  "data": {
    "teacherId": 1,
    "subjects": []
  }
}
```

---

### 2. Save Full Subject (Standard Domain)

Teacher can teach the entire subject.

**Request:**
```http
POST /Api/V1/Teacher/TeacherSubject
Authorization: Bearer <token>
Content-Type: application/json
```

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 1,
      "curriculumId": 1,
      "levelId": 1,
      "gradeId": 1,
      "canTeachFullSubject": true,
      "units": []
    }
  ]
}
```

**Expected Response (200 OK):**
```json
{
  "succeeded": true,
  "message": "Teacher subjects saved successfully",
  "data": {
    "teacherId": 1,
    "subjects": [
      {
        "id": 1,
        "subjectId": 1,
        "subjectNameAr": "الرياضيات",
        "subjectNameEn": "Mathematics",
        "domainCode": "education",
        "curriculumId": 1,
        "curriculumNameAr": "المنهج السعودي",
        "canTeachFullSubject": true,
        "isActive": true,
        "units": []
      }
    ]
  }
}
```

---

### 3. Save Specific Units (Standard Domain)

Teacher can only teach specific units.

**Request:**
```http
POST /Api/V1/Teacher/TeacherSubject
Authorization: Bearer <token>
Content-Type: application/json
```

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 1,
      "curriculumId": 1,
      "levelId": 1,
      "gradeId": 1,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 1 },
        { "unitId": 2 },
        { "unitId": 3 }
      ]
    }
  ]
}
```

---

### 4. Save Quran Subject - Full (All ContentTypes & Levels)

Teacher can teach all Quran content types and all levels.

**Request:**
```http
POST /Api/V1/Teacher/TeacherSubject
Authorization: Bearer <token>
Content-Type: application/json
```

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": true,
      "units": []
    }
  ]
}
```

---

### 5. Save Quran Subject - Specific Units with All Specializations

Teacher teaches specific Surahs/Parts with all content types and levels.

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": null, "quranLevelId": null },
        { "unitId": 116, "quranContentTypeId": null, "quranLevelId": null }
      ]
    }
  ]
}
```

**Note:** `null` means "all types" or "all levels"

---

### 6. Save Quran Subject - Specific ContentType Only

Teacher teaches Memorization (حفظ) only for specific units.

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": null },
        { "unitId": 116, "quranContentTypeId": 1, "quranLevelId": null }
      ]
    }
  ]
}
```

**QuranContentType IDs:**
- 1 = حفظ (Memorization)
- 2 = تلاوة (Recitation)
- 3 = تجويد (Tajweed)

---

### 7. Save Quran Subject - Specific Level Only

Teacher teaches all content types but only Beginner level.

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": null, "quranLevelId": 2 },
        { "unitId": 116, "quranContentTypeId": null, "quranLevelId": 2 }
      ]
    }
  ]
}
```

**QuranLevel IDs:**
- 1 = نوراني (Noorani)
- 2 = مبتدئ (Beginner)
- 3 = متوسط (Intermediate)
- 4 = متقدم (Advanced)

---

### 8. Save Quran Subject - Multiple Specializations for Same Unit

Teacher can teach the same unit with different specializations (creates multiple records).

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": 2 },
        { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": 3 },
        { "unitId": 115, "quranContentTypeId": 2, "quranLevelId": 2 }
      ]
    }
  ]
}
```

This creates 3 records for unit 115:
- Memorization + Beginner
- Memorization + Intermediate
- Recitation + Beginner

---

### 9. Save Multiple Subjects at Once

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 1,
      "curriculumId": 1,
      "canTeachFullSubject": true,
      "units": []
    },
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": 1, "quranLevelId": null }
      ]
    }
  ]
}
```

---

## Error Scenarios

### 10. Unauthorized (No Token)

**Request:**
```http
GET /Api/V1/Teacher/TeacherSubject
```

**Expected Response (401 Unauthorized)**

---

### 11. Teacher Not Found

If token belongs to a user without teacher profile.

**Expected Response (404 Not Found):**
```json
{
  "succeeded": false,
  "message": "Teacher not found"
}
```

---

### 12. Invalid Subject ID

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 99999,
      "canTeachFullSubject": true,
      "units": []
    }
  ]
}
```

**Expected Response (400 Bad Request):**
```json
{
  "succeeded": false,
  "message": "Subject with ID 99999 not found"
}
```

---

### 13. Missing Units When CanTeachFullSubject is False

**Body:**
```json
{
  "subjects": [
    {
      "subjectId": 1,
      "canTeachFullSubject": false,
      "units": []
    }
  ]
}
```

**Expected Response (400 Bad Request):**
```json
{
  "succeeded": false,
  "message": "Units are required when CanTeachFullSubject is false for subject الرياضيات"
}
```

---

## Postman Collection Setup

### Environment Variables

```json
{
  "baseUrl": "https://localhost:7001",
  "teacherToken": "<jwt_token_here>"
}
```

### Pre-request Script (Auto Token)

```javascript
// If you have login endpoint, auto-refresh token
if (!pm.environment.get("teacherToken")) {
    pm.sendRequest({
        url: pm.environment.get("baseUrl") + "/Api/V1/Auth/Teacher/VerifyOtp",
        method: 'POST',
        header: { 'Content-Type': 'application/json' },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                phoneNumber: "+966500000001",
                otpCode: "123456"
            })
        }
    }, function (err, res) {
        pm.environment.set("teacherToken", res.json().data.token);
    });
}
```

---

## Database Verification

### Check Saved Data

```sql
-- View teacher subjects
SELECT ts.*, s.NameAr as SubjectName
FROM teacher.TeacherSubjects ts
JOIN education.Subjects s ON ts.SubjectId = s.Id
WHERE ts.TeacherId = @TeacherId;

-- View teacher subject units with Quran specialization
SELECT tsu.*, cu.NameAr as UnitName, 
       qct.NameAr as ContentType, ql.NameAr as Level
FROM teacher.TeacherSubjectUnits tsu
JOIN education.ContentUnits cu ON tsu.UnitId = cu.Id
LEFT JOIN quran.QuranContentTypes qct ON tsu.QuranContentTypeId = qct.Id
LEFT JOIN quran.QuranLevels ql ON tsu.QuranLevelId = ql.Id
WHERE tsu.TeacherSubjectId IN (
    SELECT Id FROM teacher.TeacherSubjects WHERE TeacherId = @TeacherId
);
```

---

## Quick Test Checklist

- [ ] GET subjects returns empty for new teacher
- [ ] POST saves full subject successfully
- [ ] POST saves specific units successfully
- [ ] POST saves Quran with ContentType specialization
- [ ] POST saves Quran with Level specialization
- [ ] POST saves multiple specializations for same unit
- [ ] POST replaces existing subjects (batch operation)
- [ ] GET returns saved subjects with all details
- [ ] Error: Missing units when CanTeachFullSubject=false
- [ ] Error: Invalid subject ID
- [ ] Error: Unauthorized without token
