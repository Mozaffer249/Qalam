# Teacher Availability & Teacher Subjects — API Guide

> **Teacher subjects wizard (`filter-options`):**  
> [`Qalam.Data/AppMetaData/docs/Education_Business_Logic.md`](../Qalam.Data/AppMetaData/docs/Education_Business_Logic.md)

That file is the **filter-options** reference (trees, `nextStep`, query params, per-domain examples).

**Registration flow (v2):** [Teacher-Registration-Flow.md](Teacher-Registration-Flow.md) — subjects are added **before** account activation; each offering starts **Pending** until admin approves.

**Still documented separately:**

- `POST` / `GET` `/Api/V1/Teacher/TeacherSubject` — save/list teacher offerings (after `filter-options` returns `Done`; allowed before `Active` except when `Blocked`)
- `GET` / `POST` `/Api/V1/Teacher/TeacherAvailability` — weekly schedule and exceptions
- [DATE_RANGE_AND_AVAILABILITY.md](../DATE_RANGE_AND_AVAILABILITY.md) — student calendar booking
- [CreateCourse.md](./CreateCourse.md) — courses after subjects are set

## Subject wizard (teacher survey `/teacher/survey?phase=subjects`)

The UI uses shared helpers under `apps/teacher/src/lib/education/`:

| Module | Role |
|--------|------|
| `educationTreeSteps.ts` | Map `EducationRule` → ordered steps; Quran detection |
| `filterOptionsClient.ts` | Typed `GET /Education/filter-options` with camelCase params |
| `useFilterOptionsWizard.ts` | Group-creation loop (Curriculum → … → Subject) |

### Standard (school) flow

1. **Group** — wizard steps from `rule` until `nextStep === Subject`; save group filter state.
2. **Subjects** — `filter-options` with group params → pick subject.
3. **Terms** — when `rule.hasAcademicTerm`, show term multi-select; **Skip** loads units for all terms (same as selecting every term ID).
4. **Units** — `unit[]` from filter-options; full subject or specific `unitIds` on `POST TeacherSubject`.

Optional **Lesson** step applies when drilling with `contentUnitId` (course/session builders); teacher subject save stops at unit selection.

### Quran flow

When `rule.requiresQuranContentType` / `requiresQuranLevel` (or legacy `domainCode === 'quran'`), the Quran picker loads paginated `unit[]` plus `contentTypes` and `levels` per unit before POST.

### Verification checklist

- [ ] Group steps match domain `EducationRule` flags (skills domain skips curriculum)
- [ ] Term picker appears when `hasAcademicTerm`; skip shows all units
- [ ] Query params use camelCase (`termIds`, not `TermIds`)
- [ ] Quran POST includes `quranContentTypeId` / `quranLevelId` per unit when required

Admin catalog management for the same tree: [Education-Management-CRUD.md §12 — Admin content tree](Education-Management-CRUD.md).
