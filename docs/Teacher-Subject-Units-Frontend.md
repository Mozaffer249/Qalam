# Teacher subject units — frontend guide

> For student / Flutter / web FE building Open Session Request content pickers.

## What to call

```text
1) GET /Api/V1/Student/Teachers/{teacherId}/Subjects
2) GET /Api/V1/Student/Teachers/{teacherId}/Subjects/{teacherSubjectId}/Units
3) GET /Api/V1/Content/Lessons?contentUnitId={unitId}&pageSize=100   (optional)
```

Do **not** use `GET /Content/Units?subjectId=` for this flow.

## Why two steps?

| Call | Purpose |
|------|---------|
| **Subjects** | List what the teacher teaches (`subjectId`, `domainId`, `teacherSubjectId`) |
| **Units** | List units this teacher is allowed to teach for **that** TeacherSubject |

`Subjects.units[]` is only a preview. When `canTeachFullSubject: true` it is often **empty** — that is normal. Always load the content picker from **Units**.

## Step-by-step

### 1. Load subjects

```http
GET /Api/V1/Student/Teachers/10/Subjects?limit=10
```

Pick a row. Keep:

- `teacherSubjectId` — required for Units
- `subjectId` — required for create OSR body
- `domainId` — required for create OSR body
- `canTeachFullSubject` — UI only (optional badge)

### 2. Load units (after subject pick)

```http
GET /Api/V1/Student/Teachers/10/Subjects/{teacherSubjectId}/Units
```

Response shape (`data[]`):

| Field | Use as |
|-------|--------|
| `id` | `contentUnitId` in create payload |
| `nameAr` / `nameEn` | Labels |
| `quranContentTypeId` / `quranLevelId` | Quran create fields when present |

Server behavior (you do not branch this yourself):

- Full subject → all catalog units for that TeacherSubject’s subject
- Partial → only units the teacher saved

### 3. Load lessons (optional)

```http
GET /Api/V1/Content/Lessons?contentUnitId={id}&pageSize=100
```

Use query param **`contentUnitId`** (not `unitId`).

## Create mapping

| Create field | From |
|--------------|------|
| `targetedTeacherId` | Teacher card / profile `id` |
| `subjectId` | Subjects row |
| `domainId` | Subjects row |
| `sessions[].units[].contentUnitId` | Units `id` |
| `sessions[].units[].lessonId` | Lessons `id` (if picking a lesson) |
| `sessions[].units[].includesAllLessons` | `true` if whole unit; `false` if lesson |

## Flutter (Qalam app)

Already wired:

1. `selectSubject(..., teacherSubjectId: …)` stores id on wizard draft
2. `teacherSubjectUnitsProvider((teacherId, teacherSubjectId))` calls Units
3. `ContentSelectionScreen` uses that provider (not `subject.units`)
4. Lessons use `contentUnitId`

Endpoints helper:

```dart
ApiEndpoints.teacherSubjects(teacherId)
ApiEndpoints.teacherSubjectUnits(teacherId, teacherSubjectId)
ApiEndpoints.contentLessons  // + query contentUnitId
```

## Quick checklist

- [ ] After subject tap, call Units with `teacherSubjectId` from Subjects
- [ ] Treat empty Subjects `units[]` as OK when full-subject
- [ ] Map Units `id` → create `contentUnitId`
- [ ] Lessons: `contentUnitId`, not `unitId`
- [ ] Never load picker from raw catalog `subjectId` alone

## Related

Full API reference: [STUDENT-REQUEST-TEACHER.md](STUDENT-REQUEST-TEACHER.md) (§4.4 / §4.4b)
