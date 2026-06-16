# Teacher Availability & Teacher Subjects — API Guide

> **Teacher subjects wizard (`filter-options`):**  
> [`Qalam.Data/AppMetaData/docs/Education_Business_Logic.md`](../Qalam.Data/AppMetaData/docs/Education_Business_Logic.md)

That file is the **filter-options** reference (trees, `nextStep`, query params, per-domain examples).

**Still documented separately:**

- `POST` / `GET` `/Api/V1/Teacher/TeacherSubject` — save/list teacher offerings (after `filter-options` returns `Unit`)
- `GET` / `POST` `/Api/V1/Teacher/TeacherAvailability` — weekly schedule and exceptions
- [DATE_RANGE_AND_AVAILABILITY.md](../DATE_RANGE_AND_AVAILABILITY.md) — student calendar booking
- [CreateCourse.md](./CreateCourse.md) — courses after subjects are set
