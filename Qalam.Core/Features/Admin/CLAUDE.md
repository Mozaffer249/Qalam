# Admin Feature

Admin back-office: teacher lifecycle, finance (payouts/refunds/revenue), pricing configuration, session management, complaints, payments reconciliation, LRS, legal documents, system settings, and reference data management.

---

## Sub-features

| Sub-feature | Path | Description |
|-------------|------|-------------|
| Teacher Management | `Commands/`, `Queries/` | Activate/block/delete teachers, approve documents/domains, bulk activation, export |
| Finance | `Finance/` | Payouts (batch lifecycle), refunds, revenue, earnings recompute, teacher finance summaries |
| Pricing | `Pricing/` | Domain session prices, teacher levels/tiers, markets, exchange rates, share overrides, level upgrades |
| Sessions | `Sessions/` | View sessions, complaints, resolve with finance side effects (refund/hold/void/release earnings) |
| Payments | `Payments/` | Payment reconciliation runs, payment events |
| LegalDocuments | `LegalDocuments/` | Create/update legal docs with versioned sections, publish/unpublish |
| LRS | `Lrs/` | NELC xAPI statement dashboard, retry failed, dispatch enrollment, settings |
| Nationalities | `Nationalities/` | CRUD nationality reference data |
| TeacherDomainQuestions | `TeacherDomainQuestions/` | CRUD per-domain qualification questions for teacher registration |
| TeacherRegistrationRequirements | `TeacherRegistrationRequirements/` | CRUD registration document requirements |
| TeacherSubjects | `TeacherSubjects/` | Activate/inactivate/restore teacher subject approvals |
| Students | `Students/` | View student details and list |
| StudentWallets | `StudentWallets/` | View/manage student wallet balances |
| PolicyCases | `PolicyCases/` | Cancellation policy case management |
| CancellationPolicy | `CancellationPolicy/` | Version and manage cancellation policies |
| System Settings | `Commands/` | Auth settings, payment gateway, OSR notifications, teacher access settings |
| Contact Messages | `Commands/`, `Queries/` | Contact form submissions: close, reopen, set in-progress |

---

## Business rules

### Teacher lifecycle
1. **Pending** → admin reviews uploaded documents → Approve/Reject each document.
2. All documents approved → `ActivateTeacherAccountCommand` → teacher active, can create courses.
3. Per-domain: teacher answers domain questions → admin approves submission → domain approved.
4. `BulkActivatePartialDomainTeachersCommand` — batch-activate teachers who have at least one approved domain.
5. `BlockTeacherCommand` / `DeleteTeacherCommand` — deactivation with cascading effects on enrollments.

### Finance — Payout batch lifecycle
`Create` → `SubmitForReview` → `Approve` → `Process` → `MarkPaid` (happy path).
Branch: `Reject` (back to draft), `Cancel`, `MarkFailed` → `Retry`.

### Finance — Refunds
`IssueAdminRefundCommand` creates refund → `ApproveAdminRefundCommand` / `RejectAdminRefundCommand`.
Refund goes to student wallet by default. Admin can override to original payment method.

### Finance — Earnings recompute
`RecomputeTeacherEarningsCommand` recalculates a teacher's earnings for an enrollment (used after pricing corrections or complaint resolutions).

### Session complaints — Resolution with side effects
`ResolveSessionComplaintCommand` can trigger financial side effects:
- `IssueAdminSessionRefundCommand` — refund student for a session.
- `HoldAdminSessionEarningCommand` — hold teacher earning pending investigation.
- `VoidAdminSessionEarningCommand` — void teacher earning (teacher not paid).
- `ReleaseAdminSessionEarningCommand` — release held earning (teacher paid normally).
- `WarnAdminSessionTeacherCommand` / `BlockAdminSessionTeacherCommand` — disciplinary.

### Pricing
- `SetDomainSessionPriceCommand` — base price per domain (platform-wide).
- `SetTeacherDomainPricingCommand` — per teacher: level, custom prices, reflect platform flags, share %.
- `SetTeacherLevelCommand` — assign teacher to a level tier.
- `SetTeacherShareOverrideCommand` — override default share for a specific teacher.
- Teacher level tiers define price ranges. `ApproveLevelUpgradeSuggestionCommand` / `RejectLevelUpgradeSuggestionCommand` for system-suggested upgrades.
- `UpdatePricingExchangeRateCommand` — currency conversion rates.
- `CreatePricingMarketCommand` / `SetDefaultPricingMarketCommand` — market-specific pricing.

### System settings (admin-switchable)
- `UpdatePaymentGatewaySettingsCommand` — switch active gateway (Mock/Moyasar/PayTabs/HyperPay/Stripe).
- `UpdateAuthSettingsCommand` — auth configuration.
- `UpdateOsrNotificationSettingsCommand` — OSR broadcast matching settings.
- `UpdateTeacherAccessSettingsCommand` — teacher platform access rules.

---

## Key entities

- `AdminPayoutBatch` / `AdminPayoutBatchItem` — payout lifecycle
- `AdminRefund` — refund requests with approval flow
- `AdminRevenueRecord` — platform revenue per enrollment
- `TeacherEarning` — per-session teacher earning (Pending/Held/Paid/Voided)
- `LegalDocument` / `LegalDocumentVersion` / `LegalDocumentSection` — versioned legal content
- `LrsStatement` — xAPI statement records for NELC compliance
- `SessionComplaint` / `ComplaintResolution` — complaint + resolution with finance side effects
- `TeacherLevel` / `TeacherLevelTier` — pricing tier system
- `PricingMarket` / `PricingExchangeRate` — multi-market pricing
- `DomainSessionPrice` — base price per education domain

---

## Gotchas

- Payout batch has a strict state machine — calling commands out of order returns 400.
- `RecomputeTeacherEarningsCommand` is destructive to prior earning calculations — use only when pricing was genuinely wrong.
- Complaint resolution preview (`GetComplaintResolvePreviewQuery`) should be shown before resolving to confirm financial impact.
- `BackfillStarterTeacherLevelsCommand` is a one-time migration command — sets default levels for teachers missing them.
- `SeedEmailSuppressionsCommand` is for email deliverability management — populates suppression list.
- System settings changes take effect immediately — no confirmation step in the backend.
- `GetAdminDashboardOverviewQuery` aggregates across multiple tables — can be slow on large datasets.
