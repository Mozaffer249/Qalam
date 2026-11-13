# Student Feature

Student and guardian-facing functionality: course browsing, enrollment, payments, sessions, wallet, OSR, and children management.

---

## Sub-features

| Sub-feature | Path | Description |
|-------------|------|-------------|
| CourseCatalog | `CourseCatalog/Queries` | Browse/search published courses, recommendations |
| EnrollmentRequests | `EnrollmentRequests/` | Group enrollment requests, invite flow, member response |
| Enrollments | `Enrollments/` | Individual enrollment, cancel, list/detail |
| OpenSessionRequests | `OpenSessionRequests/` | Scenario 2: create/publish OSR, manage offers, invitations |
| Payments | `Payments/` | Payment intents, card payment, wallet payment, receipts |
| Wallet | `Wallet/` | Wallet balance, top-up, transactions |
| Sessions | `Sessions/` | Join sessions, complaints, reviews, live tokens |
| Teachers | `Teachers/Queries` | Browse teacher profiles, availability, reviews, certificates |
| Children | `Commands/` | Guardian adds/updates children |
| Identity | `Commands/` | NELC national identity (NationalId/Iqama/Passport). **Required at registration** (Flutter self-register + add-child block submit without it) — it's the xAPI `actor.name`. NationalId: 10 digits starting 1/2/4. See [docs/LRS.md](../../../docs/LRS.md). |

---

## Business rules

### Enrollment
- **Fixed courses only** — flexible rejected by API.
- **Individual** → `CreateIndividualEnrollmentCommand` → Enrollment directly (no request). Free (`AmountDue <= 0`) → Active immediately.
- **Group** → `RequestCourseEnrollmentCommand` → `CourseEnrollmentRequest` → invite flow → payment.
- **Single payer** — owner pays full AmountDue. Invitees never pay.
- **Invite flow:** owned students (self + guardian children) auto-`Accepted`. External invitees `Pending` until respond or expire (48h default).
- **Cancel:** owner can cancel PendingPayment enrollment or unpaid request. Paid enrollments → refund to wallet.

### Payments
- `CreatePaymentIntentCommand` → creates intent with active gateway (Mock/Moyasar/PayTabs/HyperPay/Stripe).
- `ConfirmPaymentCommand` → confirms after client-side payment. Activates enrollment + generates schedules.
- `PayWithWalletCommand` → full wallet payment (no split). Debit wallet → activate.
- `PayEnrollmentParticipantCommand` → legacy mock payment path.
- Free-trial: one lifetime free session per student. Deducted from AmountDue. Teacher still paid.

### Open Session Requests (Scenario 2)
- Student creates draft → uploads attachments → publishes (broadcast or targeted at a teacher).
- Broadcast: matching teachers notified. Targeted: specific teacher, price frozen at create.
- Invitations: same model as group enrollments (owned auto-accept, external pending).
- Student reviews offers from teachers → accepts one → payment → enrollment created.

### Sessions
- Student joins session during scheduled window → marked Present.
- Can submit review after session Completed.
- Can file complaint (one open per session): teacher no-show, quality, technical.

### Wallet
- One SAR wallet per paying account (guardian's wallet pays for all children).
- Top-up via card (`CreateWalletTopUpCommand`) or mock (`MockWalletTopUpCommand`).
- All refunds go to wallet (except admin manual refund to original method).

---

## Key entities

- `Enrollment` — `OwnerUserId`, `PaidByUserId`, `EnrollmentStatus`, `AmountDue`, `PricingSnapshotId`, `PolicyVersionId`
- `CourseEnrollmentRequest` — group request with `CourseRequestGroupMember` rows
- `OpenSessionRequest` — student's session request (Scenario 2)
- `Payment` — payment record, links to gateway
- `StudentWallet` / `WalletTransaction` — wallet balance and ledger
- `CourseSchedule` — individual session instances with attendance
- `Student` — `HasUsedFreeTrialSession`, `IdentityType`, `IdentityNumber`

---

## Gotchas

- Individual enrollment does NOT go through EnrollmentRequests — different endpoint entirely.
- `GetMyInvitationsQuery` returns both course and OSR invitations (unified inbox).
- Guardian sees child's invitations; child account does NOT see their own.
- Pricing is frozen at enrollment creation in `PricingSnapshot` — never re-computed later.
- `SearchStudentsForGroupQuery` is for finding students to invite; `SearchStudentsQuery` is a general search.
- Wallet `MockWalletTopUpCommand` only works when Mock gateway is active.
