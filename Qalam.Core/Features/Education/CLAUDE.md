# Education Feature

Education hierarchy and reference data: domains, levels, grades, academic terms, writable filters, and university structure (universities, colleges, departments, academic programs).

---

## Sub-features

| Sub-feature | Path | Description |
|-------------|------|-------------|
| Domains | `Commands/`, `Queries/` | Education domain CRUD (school, Quran, languages, skills), toggle active status |
| Levels | `Commands/`, `Queries/` | Education level CRUD within domains |
| Grades | `Commands/`, `Queries/` | Grade CRUD within levels |
| Academic Terms | `Commands/`, `Queries/` | Academic term/semester CRUD |
| Writable Filters | `Commands/`, `Queries/` | Dynamic filter values (upsert, update, delete) for course catalog filtering |
| Universities | `Universities/` | University CRUD with active toggle |
| Colleges | `Colleges/` | College CRUD under universities |
| Departments | `Departments/` | Department CRUD under colleges |
| Academic Programs | `AcademicPrograms/` | Academic program CRUD under departments |

---

## Hierarchy

```
EducationDomain (school, quran, languages, skills)
  └── EducationLevel
        └── Grade
              └── AcademicTerm

University
  └── College
        └── Department
              └── AcademicProgram
```

Domains are the top-level grouping. Courses belong to a domain and optionally to a level/grade/term. The university hierarchy is separate — used for teacher qualifications and student enrollment context.

---

## Business rules

- **Domain codes** — never hardcode entity IDs. Use `code` values (`school`, `quran`, `languages`, `skills`) for lookups.
- **Active/inactive toggle** — `ToggleEducationDomainStatusCommand` flips domain visibility. Inactive domains hidden from course catalog but existing courses/enrollments unaffected.
- **Cascade awareness** — deleting a level does NOT cascade-delete grades. Validation prevents deletion if children exist.
- **Writable filters** — dynamic key-value pairs for course catalog filtering (e.g., teaching language, certification type). `UpsertWritableFilterValueCommand` creates or updates. Separate from the fixed domain/level/grade hierarchy.
- **Validation** — `EducationRuleCommandValidator.cs` in the feature root contains shared validation rules across education commands (e.g., unique codes, valid parent references).
- **University hierarchy** — soft-delete pattern via `SetActive` commands. Universities/colleges/departments/programs are reference data, not directly linked to course scheduling.

---

## Key entities

- `EducationDomain` — top-level: `Code`, `NameEn`, `NameAr`, `IsActive`, `SortOrder`
- `EducationLevel` — belongs to domain: `Code`, `NameEn`, `NameAr`
- `Grade` — belongs to level
- `AcademicTerm` — belongs to grade (semester/quarter)
- `WritableFilterValue` — dynamic catalog filter: `FilterKey`, `Value`, `DisplayNameEn`, `DisplayNameAr`
- `University` / `College` / `Department` / `AcademicProgram` — reference hierarchy for qualifications

---

## Gotchas

- `GetFilterOptionsQuery` returns all active filter options for the course catalog — used by frontend filter dropdowns, not just writable filters.
- `GetEducationDomainsListQuery` returns all domains (active and inactive) for admin; course catalog queries filter to active only.
- Education entities use both `Id` (int PK) and `Code` (string, unique) — always prefer `code` for lookups and API references.
- The `EducationRuleCommandValidator.cs` at the feature root is a shared validator, not a command or query — don't confuse it with the per-command validators in sub-folders.
