# Teacher Availability & Teacher Subjects — API Guide

> **Teacher subjects wizard (`filter-options`):**  
> [`Qalam.Data/AppMetaData/docs/Education_Business_Logic.md`](../Qalam.Data/AppMetaData/docs/Education_Business_Logic.md)

**POST / GET contract and test scenarios:** [TeacherSubject-Testing-Guide.md](TeacherSubject-Testing-Guide.md)

**Registration flow (v2):** [Teacher-Registration-Flow.md](Teacher-Registration-Flow.md)

**Still documented separately:**

- `POST` / `GET` `/Api/V1/Teacher/TeacherSubject` — save/list teacher subjects
- `GET` / `POST` `/Api/V1/Teacher/TeacherAvailability` — weekly schedule and exceptions
- [DATE_RANGE_AND_AVAILABILITY.md](../DATE_RANGE_AND_AVAILABILITY.md) — student calendar booking
- [CreateCourse.md](./CreateCourse.md) — courses after subjects are set

## Subject wizard (teacher survey `/teacher/survey?phase=subjects`)

Single page: **domain filter bar** at top, rule-driven step strip, multi-select panel, batch summary, **pending subjects list**, and **Continue** (one POST).

### Components (`apps/teacher/src/routes/teacher/survey/-components/`)

| Component | Role |
|-----------|------|
| `SubjectSelection.tsx` | Orchestrator — domain, wizard state, POST |
| `DomainFilterBar.tsx` | Search + select domain (`GET /Education/Domains`) |
| `EducationStepStrip.tsx` | Rule-driven steps + path tags |
| `EducationMultiSelectPanel.tsx` | Checkbox multi-select; Quran branch |
| `SelectionSummary.tsx` | Current batch summary + **Add subjects** |
| `PendingSubjectsList.tsx` | Flat list of subjects to save; remove per row |

### Layout

1. **Top** — Domain search + dropdown; step strip from `teacherSurveyStepsFromRule(rule)` (no Term or Lesson steps)
2. **Main** — Active-step panel (single-select path steps; multi-select Subject and Unit)
3. **Side** — Batch summary + **Add subjects**; pending subjects list
4. **Footer** — **Continue** → `POST /Api/V1/Teacher/TeacherSubject`

### School domain: prefetch + inline terms

- When a subject is toggled **on**, the client prefetches its catalog via `filter-options` (terms or units depending on `nextStep`).
- **Term** is not a strip step; term checkboxes appear **inline on the Unit step** when `hasAcademicTerm` applies.
- Changing terms (or **Show all units / all terms**) refetches units for affected subjects only.
- **Lesson** is omitted from the teacher survey — repertoire is saved at **unit level** only (`POST /TeacherSubject` has no `lessonId`).

### Skills / language domains

Subjects without academic terms prefetch units immediately on toggle; the Unit step shows unit checkboxes with no term filter.

### Pending subjects (no named batches)

Each **Add subjects** appends one row per subject (`PendingTeacherSubject`) with hierarchy `path`, units, and optional Quran `unitSpecs`.

Changing domain resets the wizard panel only; **pending subjects from other domains are kept** until Continue.

### POST shape

Matches [TeacherSubject-Testing-Guide.md](TeacherSubject-Testing-Guide.md):

```json
{
  "subjects": [
    {
      "subjectId": 1,
      "canTeachFullSubject": false,
      "units": [{ "unitId": 10 }]
    },
    {
      "subjectId": 499,
      "canTeachFullSubject": false,
      "units": [
        { "unitId": 115, "quranContentTypeId": null, "quranLevelId": 2 }
      ]
    }
  ]
}
```

Quran: `null` on `quranContentTypeId` / `quranLevelId` means all types / all levels (see testing guide scenarios 5–7).

The teacher survey does not expose a Lesson step; units are selected directly.

### Verification checklist

- [ ] Survey opens directly on subject wizard (no separate domain page)
- [ ] Domain dropdown at top; panel resets on domain change; pending list preserved
- [ ] Add subjects → flat pending list → Continue POST
- [ ] Mixed-domain pending batch posts in one request
- [ ] Quran null content-type/level allowed
- [ ] School: units prefetched on subject select; terms inline on Unit step; no Lesson in strip
- [ ] Skills: units shown on Unit step without term UI
- [ ] No UI copy uses "group" / "مجموعة" for this flow

Admin catalog tree: [Education-Management-CRUD.md §12](Education-Management-CRUD.md).
