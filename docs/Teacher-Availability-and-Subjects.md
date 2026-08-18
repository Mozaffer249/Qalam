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
2. **Subjects** — nested checkbox tree from `EducationRule` (not shared flat tags)  
3. **Coverage** — leftovers shared on the subject (Language skill/purpose/curriculum; Quran content type / level)  
4. **Review & add** — batch summary + **Add subjects**; pending list always visible; **Continue** posts when pending is non-empty  

**UI + pending row** show the nested path down to **Subject**. **POST** is **one row per subject** with `canTeachFullSubject: true` and empty `units` (and optional `writableFilterValueIds`). Level/Grade are not API columns.

Checking a node fetches **that node’s** children only (one parent id per `filter-options` request). Unchecking drops the subtree. The tree **stops at Subject**. The survey does **not** call `filter-options` for Unit / Term / Lesson, even if the API `nextStep` is one of those — treat it as Done. Default is teach the full subject (all units, all terms).

### Components (`apps/teacher/src/routes/teacher/survey/-components/`)

| Component | Role |
|-----------|------|
| `SubjectSelection.tsx` | Orchestrator — tree state, leftover coverage, POST |
| `SurveyPathTree.tsx` | Recursive nested checkboxes + lazy children (no unit leaf) |
| `SurveyPhaseStepper.tsx` | Numbered Domain → Subjects → Coverage → Review (full-width top) |
| `DomainFilterBar.tsx` | Search + select domain (`GET /Education/Domains`) |
| `EducationMultiSelectPanel.tsx` | Quran coverage leftovers + leftover writables |
| `SelectionSummary.tsx` | Batch summary + **Add subjects** (phase 4) + blocked reasons |
| `PendingSubjectsList.tsx` | One row per subject with tree summary text |

### Layout

1. **Top** — Phase stepper (Step N of 4)  
2. **Center** — Current phase content + Back/Next  
3. **Side** — Draft summary + pending list (always visible)  
4. **Footer** — **Continue** → `POST /Api/V1/Teacher/TeacherSubject`

Axis names always come from `teacherSurveyStepsFromRule(rule)` / `treeStepsFromRule(rule)` — never hardcoded Language-only steps. Those lists exclude Unit / Term / Lesson.

### Nested samples by domain

#### 1. Language (`language`) — Subject → Age → CEFR; writables on the subject

Teacher: *أدرس الإنجليزية للأطفال والشباب، من A1 إلى C1، وأقدم المحادثة والقراءة والكتابة والقواعد.*

```text
الإنجليزية
 ├── أطفال
 │    ├── A1
 │    ├── A2
 │    └── B1
 ├── شباب
 │    ├── B1
 │    ├── B2
 │    └── C1
 └── بالغون   (unchecked — not in this batch)
المهارات على المادة: محادثة، قراءة، كتابة، قواعد
الغرض / التخصص: التأسيس اللغوي
المنهج: اختياري كتابة (مثال: Oxford book)
```

French can be a second root in the same domain session. POST: one row per language `subjectId` + `writableFilterValueIds`. `canTeachFullSubject: true`, empty units.

Pending summary example: `أطفال (A1, A2, B1) · شباب (B1, B2, C1) · محادثة، قراءة، كتابة، قواعد`.

#### 2. School (`school`) — Curriculum → Level → Grade → Subject

Teacher: *أدرس الرياضيات للابتدائي (صف 4–6) والمتوسط (صف 7–8).*

```text
المنهج الوطني
 ├── ابتدائي
 │    ├── صف 4
 │    │    └── الرياضيات
 │    └── صف 6
 │         └── الرياضيات
 └── متوسط
      └── صف 7
           └── الرياضيات
```

POST: one Math row, `canTeachFullSubject: true`, empty `units`.

#### 3. Sharia (`sharia`) — same nesting as the rule, stop at Subject

Teacher: *أدرس الفقه للمرحلة المتوسطة.*

```text
[Curriculum/Level/Grade from rule]
 └── متوسط
      └── الفقه
```

Path follows `EducationRule` (not a copy of school labels). No unit leaf.

#### 4. University (`university`) — University → College → Department → Program → Subject

Teacher: *أدرس تراكيب البيانات في جامعة X، كلية الحاسب، قسم علوم الحاسب، برنامج البكالوريوس.*

```text
جامعة الملك سعود
 └── كلية علوم الحاسب
      └── قسم علوم الحاسب
           └── بكالوريوس علوم الحاسب
                └── تراكيب البيانات
```

A second college is a **sibling branch**, not mixed into this department’s subjects. POST: one subject row, `canTeachFullSubject: true`.

#### 5. Quran (`quran`) — Subject; optional coverage leftovers

Teacher: *أدرس القرآن (كامل المادة)، حفظاً وتجويداً للمستوى المبتدئ — أو أترك المحتوى/المستوى فارغين للكل.*

```text
القرآن
 محتوى: حفظ، تجويد          (empty = all)
 مستوى: مبتدئ               (empty = all)
```

No juz/surah picker. Coverage loads types/levels from `GET /Api/V1/Quran/ContentTypes` and `GET /Api/V1/Quran/Levels` — not from `filter-options` unit lists. POST: one subject, `canTeachFullSubject: true`, empty `units`, `quranContentTypeIds` / `quranLevelIds` or `[]`.

#### 6. Skills + wave-1 (`skills`, `soft-skills`, `life-skills`, `tech-skills`, `hobbies`, `finance`, `knowledge`)

Teacher (soft-skills): *أدرس مهارات التواصل والعروض، مستوى متوسط.*

```text
مهارات التواصل
 ├── (writables from rule, e.g. الجمهور / الصيغة)
 └── مستوى متوسط
العروض التقديمية
 └── مستوى مبتدئ
```

If the rule has no Level-after-subject, the subject is the leaf. Writable chain must finish before Add. No unit leaf.

### Terms and lessons

- **Term** and **Lesson** are not survey tree steps. Teaching the full subject implies all terms.
- The survey ignores API `nextStep` of `Unit`, `Term`, or `Lesson`.

### Pending subjects

Each **Add subjects** appends **one row per subject** with `treeSummary` text (not cartesian rows) and an “all units” badge when `canTeachFullSubject`. Domain change resets the draft panel; pending from other domains is kept until Continue.

### POST shape

Survey always sends `canTeachFullSubject: true` with empty `units`:

```json
{
  "subjects": [
    {
      "subjectId": 1,
      "canTeachFullSubject": true,
      "units": []
    },
    {
      "subjectId": 499,
      "canTeachFullSubject": true,
      "units": [],
      "quranContentTypeIds": [],
      "quranLevelIds": []
    }
  ]
}
```

Language also sends `writableFilterValueIds` on that one subject row. Quran: empty `quranContentTypeIds` / `quranLevelIds` means all types / all levels.

### Verification checklist

- [ ] Four-phase stepper full-width; Arabic labels readable
- [ ] Nested tree: check a node loads **that** branch’s children; tree stops at Subject (no unit/term fetch)
- [ ] Language: English Kids A1–B1 + Youth B1–C1 + skills; pending matches tree; POST one `subjectId` + writables, `canTeachFullSubject: true`
- [ ] School: two levels with different grades under Math; POST full subject, empty units
- [ ] Sharia: nested path → subject; no unit leaf
- [ ] University: uni → college → dept → program → subject
- [ ] Quran: subject + optional content type/level; no juz/surah; full subject POST
- [ ] Skills / wave-1: subject → writables leftover → level?; no units
- [ ] Mixed-domain pending posts in one Continue request
- [ ] RTL + mobile usable

Admin catalog tree: [Education-Management-CRUD.md §12](Education-Management-CRUD.md).
