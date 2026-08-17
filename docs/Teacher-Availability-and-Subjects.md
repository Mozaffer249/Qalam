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
3. **Coverage** — leftovers shared on the subject (Language skill/purpose/curriculum; Quran 3.1–3.4)  
4. **Review & add** — batch summary + **Add subjects**; pending list always visible; **Continue** posts when pending is non-empty  

**UI + pending row** show the full nested tree. **POST** stays **one row per subject** (`canTeachFullSubject: false` + unit ids and/or `writableFilterValueIds`). Level/Grade are not API columns.

Checking a node fetches **that node’s** children only (one parent id per `filter-options` request). Unchecking drops the subtree. No «teach full subject» / full Quran; domains with units require ≥1 unit (`Select all units` on the unit leaf). Terms stay inline on the unit leaf when `hasAcademicTerm`.

### Components (`apps/teacher/src/routes/teacher/survey/-components/`)

| Component | Role |
|-----------|------|
| `SubjectSelection.tsx` | Orchestrator — tree state, leftover coverage, POST |
| `SurveyPathTree.tsx` | Recursive nested checkboxes + lazy children |
| `SurveyPhaseStepper.tsx` | Numbered Domain → Subjects → Coverage → Review (full-width top) |
| `DomainFilterBar.tsx` | Search + select domain (`GET /Education/Domains`) |
| `EducationMultiSelectPanel.tsx` | Quran seq + leftover writables |
| `SelectionSummary.tsx` | Batch summary + **Add subjects** (phase 4) + blocked reasons |
| `PendingSubjectsList.tsx` | One row per subject with tree summary text |

### Layout

1. **Top** — Phase stepper (Step N of 4)  
2. **Center** — Current phase content + Back/Next  
3. **Side** — Draft summary + pending list (always visible)  
4. **Footer** — **Continue** → `POST /Api/V1/Teacher/TeacherSubject`

Axis names always come from `teacherSurveyStepsFromRule(rule)` / `treeStepsFromRule(rule)` — never hardcoded Language-only steps.

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
المنهج: اختياري ومقيّد برمز اللغة
```

French can be a second root in the same domain session. POST: one row per language `subjectId` + `writableFilterValueIds`. Empty units.

Pending summary example: `أطفال (A1, A2, B1) · شباب (B1, B2, C1) · محادثة، قراءة، كتابة، قواعد`.

#### 2. School (`school`) — Curriculum → Level → Grade → Subject → Units

Teacher: *أدرس الرياضيات للابتدائي (صف 4–6) والمتوسط (صف 7–8)، الوحدات المحددة فقط.*

```text
المنهج الوطني
 ├── ابتدائي
 │    ├── صف 4
 │    │    └── الرياضيات
 │    │         ├── وحدة 1
 │    │         └── وحدة 2
 │    └── صف 6
 │         └── الرياضيات
 │              └── وحدة 3
 └── متوسط
      └── صف 7
           └── الرياضيات
                ├── وحدة أ
                └── وحدة ب
```

صف 4 and صف 7 must **not** share one global unit list. POST: one Math row, `canTeachFullSubject: false`, union of checked unit ids.

#### 3. Sharia (`sharia`) — same nesting as the rule

Teacher: *أدرس الفقه للمرحلة المتوسطة، وحدات العبادات والمعاملات.*

```text
[Curriculum/Level/Grade from rule]
 └── متوسط
      └── الفقه
           ├── العبادات
           └── المعاملات
```

Path follows `EducationRule` (not a copy of school labels). Select-all units on the subject leaf. No full-subject.

#### 4. University (`university`) — University → College → Department → Program → Subject → Units

Teacher: *أدرس تراكيب البيانات في جامعة X، كلية الحاسب، قسم علوم الحاسب، برنامج البكالوريوس.*

```text
جامعة الملك سعود
 └── كلية علوم الحاسب
      └── قسم علوم الحاسب
           └── بكالوريوس علوم الحاسب
                └── تراكيب البيانات
                     ├── شجرة AVL
                     └── جداول التجزئة
```

A second college is a **sibling branch**, not mixed into this department’s subjects. POST: one subject row + unit ids.

#### 5. Quran (`quran`) — Subject; coverage 3.1–3.4

Teacher: *أدرس أجزاء 1–5 حفظاً وتجويداً للمستوى المبتدئ.*

```text
القرآن
 └── نوع التصفح: أجزاء
      ├── جزء 1
      ├── جزء 2
      ├── جزء 3
      ├── جزء 4
      └── جزء 5
 محتوى: حفظ، تجويد          (empty = all)
 مستوى: مبتدئ               (empty = all)
```

No «القرآن كاملاً». Switching أجزاء/سور clears the other type’s unit picks. POST: one subject, unit ids, `quranContentTypeIds` / `quranLevelIds` or `[]`.

#### 6. Skills + wave-1 (`skills`, `soft-skills`, `life-skills`, `tech-skills`, `hobbies`, `finance`, `knowledge`)

Teacher (soft-skills): *أدرس مهارات التواصل والعروض، مستوى متوسط، الوحدات المختارة.*

```text
مهارات التواصل
 ├── (writables from rule, e.g. الجمهور / الصيغة)
 └── مستوى متوسط
      ├── الوحدة 1
      └── الوحدة 2
العروض التقديمية
 └── مستوى مبتدئ
      └── الوحدة أ
```

If the rule has no Level-after-subject, units hang directly under the subject after writables. Writable chain must finish before Add; ≥1 unit when `hasContentUnits`.

### School: inline terms

- **Term** is not a tree step; term checkboxes appear **inline on the unit leaf** when `hasAcademicTerm`.
- **Lesson** omitted — repertoire at unit level only.

### Pending subjects

Each **Add subjects** appends **one row per subject** with `treeSummary` text (not cartesian rows). Domain change resets the draft panel; pending from other domains is kept until Continue.

### POST shape

Survey always sends `canTeachFullSubject: false` with explicit unit ids when the domain has units:

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
      "units": [{ "unitId": 115 }],
      "quranContentTypeIds": [],
      "quranLevelIds": []
    }
  ]
}
```

Language also sends `writableFilterValueIds` on that one subject row. Quran: empty `quranContentTypeIds` / `quranLevelIds` means all types / all levels.

### Verification checklist

- [ ] Four-phase stepper full-width; Arabic labels readable
- [ ] Nested tree: check a node loads **that** branch’s children (no sibling-unioned unit lists)
- [ ] Language: English Kids A1–B1 + Youth B1–C1 + skills; pending matches tree; POST one `subjectId` + writables
- [ ] School: two levels with **different** grades/units under Math; no leaked units across grades
- [ ] Sharia: nested level → subject → units; Select all units
- [ ] University: uni → college → dept → program → subject → units
- [ ] Quran: 3.1–3.4 nested; no full Quran
- [ ] Skills / wave-1: subject → writables leftover → level? → units
- [ ] No full-subject / full-Quran controls; Select all units instead
- [ ] Mixed-domain pending posts in one Continue request
- [ ] RTL + mobile usable

Admin catalog tree: [Education-Management-CRUD.md §12](Education-Management-CRUD.md).
