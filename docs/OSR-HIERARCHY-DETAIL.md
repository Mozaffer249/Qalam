# OSR detail — education hierarchy

`GET /Api/V1/Student/OpenSessionRequests/{id}?lang=ar|en`

Returns **Id + localized Name** pairs for the request-level education hierarchy. Names follow `?lang=` (via request culture). **Name is null when the corresponding ID is null.**

Create / update draft accept the same hierarchy IDs (including university path). Unused fields stay null per domain.

Also see [STUDENT-REQUEST-TEACHER.md](STUDENT-REQUEST-TEACHER.md) §6.3.

## Fields

| Id | Name |
|----|------|
| `domainId` | `domainName` |
| `curriculumId` | `curriculumName` |
| `levelId` | `levelName` |
| `gradeId` | `gradeName` |
| `termId` | `termName` |
| `universityId` | `universityName` |
| `collegeId` | `collegeName` |
| `departmentId` | `departmentName` |
| `academicProgramId` | `academicProgramName` |
| `subjectId` | `subjectName` |

**Sessions (Quran):** `quranContentTypeId` / `quranContentTypeName`, `quranLevelId` / `quranLevelName`.

**Units:** already expose `contentUnitNameAr`/`En` and `lessonNameAr`/`En`.

## Per domain

| Domain code | Typically set | Typically null |
|-------------|----------------|----------------|
| `school` | domain → curriculum → level → grade → term → subject | university path |
| `university` | domain → university → college → department → academicProgram → level → subject → optional term | curriculum, grade |
| `language` | domain → level → subject | curriculum, grade, term, university path |
| `skills` | domain → subject | level, curriculum, grade, term, university path |
| `quran` | domain → subject; Quran content/level on **sessions** | curriculum, level, grade, term, university path |

## Examples

### School

```json
{
  "domainId": 1,
  "domainName": "مدرسي",
  "curriculumId": 2,
  "curriculumName": "المنهج السعودي",
  "levelId": 3,
  "levelName": "ثانوي",
  "gradeId": 12,
  "gradeName": "الصف الثاني عشر",
  "termId": 1,
  "termName": "الفصل الأول",
  "subjectId": 5,
  "subjectName": "الرياضيات",
  "universityId": null,
  "universityName": null,
  "collegeId": null,
  "collegeName": null,
  "departmentId": null,
  "departmentName": null,
  "academicProgramId": null,
  "academicProgramName": null
}
```

### University

```json
{
  "domainId": 2,
  "domainName": "جامعي",
  "universityId": 10,
  "universityName": "...",
  "collegeId": 20,
  "collegeName": "...",
  "departmentId": 30,
  "departmentName": "...",
  "academicProgramId": 40,
  "academicProgramName": "...",
  "levelId": 5,
  "levelName": "...",
  "termId": 1,
  "termName": "...",
  "subjectId": 99,
  "subjectName": "...",
  "curriculumId": null,
  "curriculumName": null,
  "gradeId": null,
  "gradeName": null
}
```

### Language

```json
{
  "domainId": 3,
  "domainName": "...",
  "levelId": 2,
  "levelName": "...",
  "subjectId": 10,
  "subjectName": "...",
  "curriculumId": null,
  "gradeId": null,
  "termId": null,
  "universityId": null,
  "collegeId": null,
  "departmentId": null,
  "academicProgramId": null
}
```

### Skills

```json
{
  "domainId": 4,
  "domainName": "...",
  "subjectId": 15,
  "subjectName": "...",
  "levelId": null,
  "curriculumId": null,
  "gradeId": null,
  "termId": null,
  "universityId": null,
  "collegeId": null,
  "departmentId": null,
  "academicProgramId": null
}
```

### Quran

```json
{
  "domainId": 5,
  "domainName": "...",
  "subjectId": 1,
  "subjectName": "...",
  "curriculumId": null,
  "levelId": null,
  "gradeId": null,
  "termId": null,
  "universityId": null,
  "collegeId": null,
  "departmentId": null,
  "academicProgramId": null,
  "sessions": [
    {
      "sequenceNumber": 1,
      "quranContentTypeId": 1,
      "quranContentTypeName": "...",
      "quranLevelId": 2,
      "quranLevelName": "...",
      "units": []
    }
  ]
}
```
