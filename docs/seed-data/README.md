# Seed data reference files

JSON samples for **manual API testing** and **admin hand-add** workflows. Most files are documentation only — they are not loaded at application startup.

**Exception:** Language and skills **sample units and lessons** are seeded at startup by [`LanguageSkillsCatalogSeeder.cs`](../../Qalam.Infrastructure/Seeding/LanguageSkillsCatalogSeeder.cs) (see [SEEDING_STRUCTURE.md](../../SEEDING_STRUCTURE.md)). The JSON catalog below is for **additional** rows beyond that startup data.

## Files

| File | Purpose |
|------|---------|
| [teacher-domain-questions.json](teacher-domain-questions.json) | Admin CRUD + teacher submit examples for domain registration questions |
| [teacher-registration-requirements.json](teacher-registration-requirements.json) | Global teacher registration requirement samples |
| [education-catalog-language-skills.json](education-catalog-language-skills.json) | Hand-add POST bodies for **language** and **skills** (startup already seeds sample units/lessons) |

## Hand-add catalog (language & skills)

### Prerequisites

1. API running (Docker: `http://localhost:8080`, or native dev port from root `.env`)
2. Admin logged in (`http://localhost:3005` with `npm run dev` in `apps/admin`)
3. Resolve domain ids: `GET /Api/V1/Education/Domains` — do not assume `language=3`, `skills=4` on remote databases

### Quick start — skills (3 POSTs)

Simplest path (no curriculum, level, or term):

1. Note `domainId` where `code === "skills"`
2. `POST /Api/V1/Subjects` with `{ domainId, nameAr, nameEn, isActive: true }`
3. `POST /Api/V1/Content/Units` with `{ subjectId, unitTypeCode: "LanguageModule", termId: null, orderIndex, nameAr, nameEn }`
4. `POST /Api/V1/Content/Lessons` with `{ unitId, orderIndex, nameAr, nameEn }`
5. Open `/domains/{skillsDomainId}/tree` → Subject node → refresh list

Full bodies: [education-catalog-language-skills.json](education-catalog-language-skills.json) → `workedExample`.

### Language path (4 POSTs)

1. Resolve `domainId` for `code === "language"`
2. `POST /Api/V1/Education/Levels` (no `curriculumId`)
3. `POST /Api/V1/Subjects` with `levelId`
4. `POST /Api/V1/Content/Units` with `unitTypeCode: "LanguageModule"`, `termId: null`
5. `POST /Api/V1/Content/Lessons`

See `samplesByDomain.language` in the JSON file.

### Admin tree UI

Same flow as REST:

1. Domain detail → **Manage content tree** → `/domains/{id}/tree`
2. Click canvas node (Level, Subject, Unit, …)
3. **Add** in step panel → create drawer POSTs to existing APIs
4. Refresh to confirm via `filter-options` list

### Unit type note

| Domain | Recommended `unitTypeCode` | Why |
|--------|---------------------------|-----|
| language | `LanguageModule` | No academic term on domain rule |
| skills | `LanguageModule` | `SchoolUnit` requires `termId`; skills has no terms |

### University domain

**Not covered** in hand-add samples. The current `university` education domain is a flat wizard only; multi-institution support is a separate epic:

→ [university-multi-tenant-outline.md](../university-multi-tenant-outline.md)

### Related docs

- [Education-Management-CRUD.md](../Education-Management-CRUD.md) — API reference + admin tree
- [SEEDING_STRUCTURE.md](../../SEEDING_STRUCTURE.md) — startup seeders for language/skills
- [Teacher-Availability-and-Subjects.md](../Teacher-Availability-and-Subjects.md) — teacher `filter-options` wizard
