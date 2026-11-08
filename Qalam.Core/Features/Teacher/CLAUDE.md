# Teacher Feature

Teacher-facing functionality: course management, enrollment tracking, session delivery, OSR offers, content library, profile, availability, finance, and registration lifecycle.

---

## Sub-features

| Sub-feature | Path | Description |
|-------------|------|-------------|
| CourseManagement | `CourseManagement/` | CRUD courses, sessions, publish/pause/reactivate, image upload |
| EnrollmentRequests | `EnrollmentRequests/` | View/approve/reject enrollment requests |
| Enrollments | `Enrollments/` | View enrollments, conversations, invoices, payment reminders |
| OpenSessionRequests | `OpenSessionRequests/` | Browse available OSRs, create/update/withdraw offers, conversations |
| Sessions | `Sessions/` | Join, start, complete, cancel, reschedule, attendance, notes, reviews, complaints, live tokens |
| Finance | `Finance/Queries` | Earnings summary, transaction list/detail |
| Pricing | `Pricing/Queries` | View domain pricings, hourly rate preview |
| Profile | `Profile/` | Bio, sample lesson media |
| Content | `Content/` | Content library: folders, items, upload, link to course sessions |
| Availability | `Commands/` | Weekly schedule, availability exceptions |
| Subjects | `Commands/` | Select subjects, manage content units per subject |
| TeacherAreas | `TeacherAreas/` | Teaching area/location management |
| TeachingPreferences | `TeachingPreferences/` | Teaching mode and session type preferences |
| Notifications | `Notifications/` | Teacher notification inbox, mark read |
| Documents | `Commands/` | Upload/reupload registration documents |
| DomainQuestions | `Commands/` | Answer domain-specific qualification questions |

---

## Business rules

### Registration lifecycle
1. Register (Auth feature) → upload documents → submit registration requirements.
2. Admin reviews → approves documents → teacher activated.
3. Per-domain: answer domain questions → admin approves → domain approved → can create courses in that domain.
4. Bulk activation available for teachers with partial domain approvals.

### Course management
- **Fixed courses only.** `isFlexible=true` rejected.
- States: Draft → Published → Paused → Reactivated(Published).
- Course has sessions (outline), subjects, teaching mode, session type.
- Course image uploaded to Alibaba OSS / Wasabi.

### Enrollment requests
- Fixed courses **auto-approve** on student submit. Teacher sees them but cannot reject (returns 400).
- Teacher can view request details, enrollment details, send payment reminders.
- Enrollment conversations: teacher ↔ student messaging per enrollment.

### Sessions
- Teacher joins → session In Progress. Completes → Completed.
- Attendance: mark students Present/Late/Absent/Excused.
- Teacher notes per session. Content linked from library to sessions.
- Cancel/reschedule own sessions (with policy rules).
- Respond to student complaints.
- Live session tokens via LiveKit.

### Open Session Requests (Scenario 2)
- Teacher browses available broadcast requests (matching by subjects/availability).
- Creates offer with proposed schedule/price → student accepts/rejects.
- Offer conversations: teacher ↔ student messaging per offer.
- Dismiss/view/reject available requests.
- Withdraw offers before acceptance.

### Finance
- View earnings summary, transaction list, transaction detail.
- Earnings per enrollment breakdown (paid/pending/voided sessions).
- Interview session: first completed session per teacher account is unpaid.

---

## Key entities

- `Teacher` — `HasCompletedInterviewSession`, `InterviewUnlockSource`, registration status fields
- `TeacherDomainPricing` — per teacher×domain: level, custom prices, reflect flags, share
- `TeacherSubject` — approved subjects the teacher can teach
- `TeacherAvailabilitySlot` / `TeacherAvailabilityException` — weekly schedule
- `TeacherContentItem` / `TeacherContentFolder` — content library
- `Course` — teacher's courses with `CourseSession` outline
- `OpenSessionOffer` — teacher's offer on a student's OSR

---

## Gotchas

- Fixed courses auto-approve — `RejectEnrollmentRequestCommand` exists but returns 400 for fixed courses.
- Teacher cannot see student payment details — only their own earning impact.
- Content linking has two paths: link to course session (template) vs link to scheduled session (instance).
- `SaveTeacherAvailabilityCommand` replaces the entire weekly schedule — not a patch.
- Domain questions are per-domain, not per-subject. Approval unlocks that entire domain.
- `GetCourseHourlyRatePreviewQuery` shows live pricing — not the frozen enrollment price.
