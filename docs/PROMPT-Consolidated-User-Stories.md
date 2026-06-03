# Prompt — Consolidated User Stories Documentation
## مرجع موحّد للسيناريوهين 1 و 2 — قائم على الكود الفعلي

---

> **Copy everything from `## ROLE` onward and paste it as your prompt to Claude Code (or any code-aware agent).**

---

## ROLE

You are a senior technical writer with full read access to the Qalam codebase (ASP.NET Core, C#, EF Core, MediatR). Your job is to produce **one** consolidated Markdown document covering **both** Scenario 1 (Course Enrollment) and Scenario 2 (Open Session Request), with **complete user stories for all three roles**: Student, Teacher, and Admin.

The output must be **grounded in the actual code**, not in BRDs alone. Where the code and the BRD disagree, the code is the source of truth — but you must flag the discrepancy explicitly.

---

## INPUT ARTIFACTS

Read these in order:

1. **`BRD-Scenario-2-Open-Session-Request.docx`** — full spec for Scenario 2 (in Arabic)
2. **`TEACHER-ROLE-Scenario2.md`** — teacher role detail for Scenario 2
3. **`ADMIN-ROLE-Scenario2.md`** — admin role detail for Scenario 2
4. **`CLAUDE-CODE-PLAN-Scenario2.md`** — implementation plan (skim only)
5. **The actual codebase** — this is the primary source. Specifically inspect:
   - `Qalam.Data/Entity/**` — entity definitions (`Course`, `CourseEnrollmentRequest`, `Enrollment`, `OpenSessionRequest`, …)
   - `Qalam.Core/Features/**` — MediatR handlers, commands, queries
   - `Qalam.Api/Controllers/**` — endpoint signatures, route prefixes, attributes
   - `Qalam.Infrastructure/Configurations/**` — EF mappings
   - `Qalam.Service/**` — domain services, `EnrollmentExpirationService`, payment/scheduling helpers
   - `Qalam.Service.Tests/**` — acceptance behavior from unit tests (limited coverage today)
   - `Qalam.Data/AppMetaData/Router.cs` — canonical API route constants

> **Note:** Scenario 1 has no role-specific MD files — you produce its user stories from the code itself. Scenario 2 has BRD + role docs but the implementation may not exist yet; in that case, user stories come from the BRD/role docs and you mark them `[planned]`.

---

## EXACT TASK

Produce **one** file: `docs/USER-STORIES-Scenarios-1-and-2.md`

It must contain user stories for:

| Scenario | Student | Teacher | Admin |
|----------|---------|---------|-------|
| **1 — Course Enrollment** | from code | from code | from code |
| **2 — Open Session Request** | from BRD + code (if any) | from `TEACHER-ROLE-Scenario2.md` + code | from `ADMIN-ROLE-Scenario2.md` + code |

### Investigation steps (do these before writing)

**Phase A — Scenario 1 code discovery (mandatory):**

1. Find Scenario 1 entities (`grep -r "CourseEnrollmentRequest\|Enrollment" Qalam.Data/Entity/Course`).
2. List every entity related to Scenario 1: `Course`, `CourseEnrollmentRequest`, `CourseSession`, `Enrollment`, `CourseSchedule`, `Payment`, etc. (There is no `CourseEnrollment` type — post-approval enrollment is `Enrollment`.)
3. List every Controller under `Api/V1/Student/`, `Api/V1/Teacher/`, `Api/V1/Admin/` related to courses.
4. For each Controller endpoint:
   - Capture HTTP method, route, authorization attribute, request/response DTO.
   - Locate the corresponding MediatR Command/Query handler.
   - Note any FluentValidation rules.
   - Note domain events raised.
5. List SignalR hub methods and events.
6. List background jobs (Hangfire / IHostedService) involved.

**Phase B — Scenario 2 code discovery (if exists):**

7. Repeat steps 1–6 for Scenario 2 (`OpenSessionRequest`, `OpenSessionOffer`, schema `sr`, …). Legacy `Qalam.Data.Entity.Session.SessionRequest` is a **different** model — do not conflate.
8. For features mentioned in the BRD but not yet in code, mark them `[planned]`.

**Phase C — Cross-cutting:**

9. Identify shared infrastructure used by both scenarios:
   - Notification system (`INotificationService`, channels)
   - Payment gateway wrapper
   - Video conferencing (Zoom integration)
   - File storage
   - Auth and JWT claims

---

## OUTPUT FILE STRUCTURE

The output file must follow this structure exactly:

```markdown
# User Stories — Scenarios 1 & 2
## قصص المستخدم الكاملة — السيناريوهان الأول والثاني

> Source of truth: codebase as of [today's date]. Where BRD and code diverge, code wins; divergences are listed in §11.

## Table of contents
1. Overview
2. Glossary
3. Roles & permissions matrix
4. Scenario 1 — Course Enrollment
   4.1 Student stories (S1-ST-xxx)
   4.2 Teacher stories (S1-TE-xxx)
   4.3 Admin stories (S1-AD-xxx)
5. Scenario 2 — Open Session Request
   5.1 Student stories (S2-ST-xxx)
   5.2 Teacher stories (S2-TE-xxx)
   5.3 Admin stories (S2-AD-xxx)
6. Shared cross-cutting stories (X-xxx)
7. Code references — quick index
8. Endpoint inventory (both scenarios)
9. Entity inventory (both scenarios)
10. Event inventory (domain events, SignalR events)
11. Discrepancies between BRD and code
12. Open questions
```

### User story format (strict)

Each story uses this template:

```markdown
### S{Scenario}-{Role}-{NNN}: <short title in Arabic>

**As** a <role>,
**I want** <action / capability>,
**so that** <business value>.

**Source:** `[code]` `code path(s)` _or_ `[BRD]` _or_ `[BRD+code]` _or_ `[planned]`

**Acceptance criteria:**
- [ ] AC1: …
- [ ] AC2: …
- [ ] AC3: …

**Endpoints:**
- `METHOD /Api/V1/.../...` → `HandlerClassName` (file: `path/HandlerClassName.cs`)

**Entities touched:** `Entity1`, `Entity2`

**Events fired:** `EventName1`, `EventName2`

**Notifications:** Push / Email / In-App / Real-time (specify which)

**Permissions:** required role/policy (e.g., `[Authorize(Roles="Student")]`)

**Status:** `implemented` | `partially implemented` | `planned` | `divergent`

**Notes:** any caveats, edge cases, or links to BRD section
```

### Numbering convention

- Scenario 1 Student stories: `S1-ST-001`, `S1-ST-002`, …
- Scenario 1 Teacher stories: `S1-TE-001`, …
- Scenario 1 Admin stories: `S1-AD-001`, …
- Scenario 2 Student: `S2-ST-001`, …
- Scenario 2 Teacher: `S2-TE-001`, …
- Scenario 2 Admin: `S2-AD-001`, …
- Cross-cutting (auth, notifications, payments): `X-001`, `X-002`, …

Aim for 15–25 stories per role per scenario (≈90–150 total). Resist padding; each story must reflect a real capability in code or a planned one.

---

## MINIMUM STORY COVERAGE (checklist)

### Scenario 1 — Student
- Browse / search courses
- View course details and sessions
- Enroll in a course
- Pay for enrollment
- View enrolled courses
- Attend a session (Zoom link, materials)
- Submit assignment
- Rate session / teacher
- Cancel enrollment (within policy)
- View invoices
- Receive notifications

### Scenario 1 — Teacher
- Create a course (fixed or flexible content)
- Set sessions and curriculum
- Set price and capacity
- Publish course
- Manage enrolled students
- Conduct sessions
- Upload materials / record sessions
- Mark attendance
- Receive payouts

### Scenario 1 — Admin
- Review and approve new courses
- Monitor enrollments
- Handle refunds
- View financial reports
- Suspend course or teacher

### Scenario 2 — Student
- Create open session request via wizard (multi-step)
- Add/remove/edit sessions in the request
- Attach files
- Invite co-students (group requests)
- Save draft / publish request
- View incoming offers
- Chat with each offering teacher
- Compare and accept one offer
- Pay
- Track scheduled sessions
- Cancel request

### Scenario 2 — Teacher
- Receive notification of matched request
- View available requests list (filters)
- View request details with availability match
- Submit / update / withdraw offer
- Chat with student
- Receive acceptance notification
- Execute scheduled sessions

### Scenario 2 — Admin
- Dashboard KPIs (active requests, conversion, revenue)
- Manage requests (suspend / reactivate / admin-edit / delete)
- Manage offers
- Resolve disputes (with chat access governance)
- Generate and export financial reports
- Edit matching rules and exclusions
- Suspend users
- View audit log

### Cross-cutting
- Authentication (JWT issuance / refresh)
- Profile management
- Notification preferences
- Email/Push delivery infrastructure
- Payment gateway integration
- Zoom meeting creation
- File upload / storage

---

## RULES FOR THE WRITER

1. **Every story tagged `implemented` MUST cite a real file path and class name** from the codebase. If you cannot find code, change the tag to `planned` and note it.
2. **Quote endpoint paths verbatim** from the controller route attributes — do not invent or normalize.
3. **Do not paraphrase to fit BRD phrasing** when code says otherwise. If `POST /Api/V1/Student/Enrollments` exists but BRD says `POST /Api/V1/Courses/Enroll`, write what the code says and add an entry to §11.
4. **Arabic for descriptions** (titles, acceptance criteria narrative); **English for technical** (endpoints, class names, JSON, error codes).
5. **No filler.** If a story has only one acceptance criterion, leave it at one.
6. **Acceptance criteria must be observable** (HTTP status, DB state, event fired, message visible to user) — not internal implementation details.
7. **Cross-references:** if S2-TE-005 depends on S2-ST-008, link them by ID in the Notes field.
8. **§7 (Code references quick index):** a flat table mapping every story ID → primary code file(s).
9. **§8 (Endpoint inventory):** a single sorted table: METHOD | PATH | HANDLER | ROLE | STORY ID(S).
10. **§9 (Entity inventory):** a single sorted table: ENTITY | NAMESPACE | RELATED STORY IDS.
11. **§10 (Event inventory):** a table: EVENT NAME | TYPE (Domain / SignalR / Integration) | PUBLISHER | SUBSCRIBERS | STORY IDS.
12. **§11 (Discrepancies):** a table: AREA | BRD SAYS | CODE SAYS | RESOLUTION (TBD / code is source / BRD is source).
13. **§12 (Open questions):** anything you couldn't determine from code or BRD — phrased as a direct question to the product owner.

---

## EXECUTION ORDER

1. Run code discovery (Phases A, B, C above) — output a short investigation log to chat
2. Stop and confirm scope with the user if you find unexpected major modules
3. Draft §§1–3 (overview, glossary, roles matrix)
4. Draft §4 (Scenario 1 stories) — Student, then Teacher, then Admin
5. Draft §5 (Scenario 2 stories) — same order
6. Draft §6 (cross-cutting)
7. Build §§7–10 (indexes) **mechanically** from the stories above — these must be consistent
8. Identify and fill §11 (discrepancies) and §12 (open questions)
9. Self-check: run the validation list below
10. Save to `USER-STORIES-Scenarios-1-and-2.md` and report stats: total stories, % implemented vs. planned, # discrepancies

---

## VALIDATION CHECKLIST (run before finishing)

- [ ] Every story has a Source tag
- [ ] Every `implemented` story cites a real file path that exists
- [ ] No two stories share an ID
- [ ] §8 endpoint inventory matches every endpoint mentioned in stories (and only those)
- [ ] §9 entity inventory matches every entity mentioned in stories
- [ ] §10 event inventory matches every event mentioned in stories
- [ ] All acceptance criteria are observable
- [ ] No story is purely a copy of a BRD bullet — each must reflect a capability
- [ ] Discrepancies section is non-empty if BRD and code diverged anywhere
- [ ] Document opens correctly as Markdown and links work

---

## DELIVERABLE

**One file:** `docs/USER-STORIES-Scenarios-1-and-2.md` saved next to the input artifacts.

At the end, post a summary message:
- Total stories
- Breakdown by scenario × role
- % implemented vs. planned vs. divergent
- Number of discrepancies and open questions
- Top 3 risks discovered during code reading

---

## TONE & STYLE

- Direct, technical, no marketing language
- Arabic where the audience is product/business; English where the audience is engineering
- Tables over prose where the data is structured
- Code references in backticks, file paths in backticks
- No emoji except as status markers where strictly useful

---

**End of prompt. Begin with Phase A code discovery.**