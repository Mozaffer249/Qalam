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

Numbered **4-phase wizard** (survey UI only; same `filter-options` + `POST /TeacherSubject`):

1. **Domain** — pick teaching domain  
2. **Subjects** — path parents (if any) then multi-select subjects  
3. **Coverage** — post-subject Excel steps (Age/CEFR/writables/units/Quran) with sub-progress `3.1 → 3.2…`  
4. **Review & add** — batch summary + **Add subjects**; pending list always visible; **Continue** posts when pending is non-empty  

Next/Back between phases; forward locked until the phase gate passes. Disabled Add shows short reasons (missing level, skill, units, …).

### Components (`apps/teacher/src/routes/teacher/survey/-components/`)

| Component | Role |
|-----------|------|
| `SubjectSelection.tsx` | Orchestrator — phases, coverage gates, POST |
| `SurveyPhaseStepper.tsx` | Numbered Domain → Subjects → Coverage → Review |
| `CoverageSubStrip.tsx` | Post-subject sub-progress inside phase 3 |
| `DomainFilterBar.tsx` | Search + select domain (`GET /Education/Domains`) |
| `EducationMultiSelectPanel.tsx` | Checkbox multi-select; Quran branch |
| `SelectionSummary.tsx` | Batch summary + **Add subjects** (phase 4) + blocked reasons |
| `PendingSubjectsList.tsx` | Flat list of subjects to save; remove per row |

### Layout

1. **Side / top** — Phase stepper (Step N of 4)  
2. **Center** — Current phase content only + Back/Next  
3. **Side** — Draft summary + pending list (always visible)  
4. **Footer** — **Continue** → `POST /Api/V1/Teacher/TeacherSubject`

### School domain: prefetch + inline terms

- When a subject is toggled **on**, the client prefetches its catalog via `filter-options` (terms or units depending on `nextStep`).
- **Term** is not a strip step; term checkboxes appear **inline on the Unit step** when `hasAcademicTerm` applies.
- Changing terms (or **Show all units / all terms**) refetches units for affected subjects only.
- **Lesson** is omitted from the teacher survey — repertoire is saved at **unit level** only (`POST /TeacherSubject` has no `lessonId`).

### Skills / language / wave-1 domains

Survey strip follows each domain’s `EducationRule` (Excel-aligned). Multi-select subjects, then complete coverage before **Add subjects**:

| Profile | Strip (teacher survey) | Add requires |
|---------|------------------------|--------------|
| School / sharia / university | Institution/curriculum/level/grade → subjects → units (terms inline) | Parents picked; each subject has units or full-subject |
| Language | Subject → Age → CEFR → skill → purpose → curriculum | Age + CEFR + skill + purpose; curriculum optional and scoped per language code |
| Wave-1 skills | Subject → writables → level (when after subject) → units | Writable chain finished; level when on strip; units or full-subject |
| Quran | Subject → (writable/audience) → units | Entries or full Quran as today |

Pending rows show path tags for level/grade/writables even when Subject is first on the strip. `POST /TeacherSubject` is unchanged (`subjectId` + units/writables); student filter-options, courses, and OSR matching are not modified.

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

- [ ] Four-phase stepper: Domain → Subjects → Coverage → Review & add
- [ ] Forward Next locked until phase complete; Back always allowed to prior phases
- [ ] Add subjects only on Review; blocked reasons shown when incomplete
- [ ] Pending list always visible; Continue POST when pending non-empty
- [ ] Domain change resets draft panel; pending from other domains kept
- [ ] Mixed-domain pending batch posts in one request
- [ ] Quran null content-type/level allowed
- [ ] Skills / wave-1: writables completed before Add; units per subject
- [ ] Language: Age+CEFR+skill+purpose required; curriculum scoped per language; multi-select languages
- [ ] School: parents then subjects in phase 2; units in coverage; terms inline on Unit; no Lesson
- [ ] RTL + mobile: stepper wraps horizontally; no “group” / “مجموعة” copy

Admin catalog tree: [Education-Management-CRUD.md §12](Education-Management-CRUD.md).
