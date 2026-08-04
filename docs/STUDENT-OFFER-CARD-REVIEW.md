# Student offers — card & Review Request (frontend)

Lean contract for implementing the student **offer card** (list) and **Review Request** (offer detail / accept) screens. No Flutter code in this delivery — APIs only.

## Screens

| Screen | When | Primary CTA |
|--------|------|-------------|
| **Offer card** | List of offers on an OSR (`ReceivingOffers` / open) | **View Offer** → detail; **Chat** via `conversationId` |
| **Review Request** | Single offer detail | **Accept & Pay**; **Chat** |

## Endpoints

| Action | Method | Path |
|--------|--------|------|
| List offers | `GET` | `/Api/V1/Student/OpenSessionRequests/{id}/Offers` |
| Offer detail | `GET` | `/Api/V1/Student/OpenSessionRequests/{id}/Offers/{offerId}` |
| Accept | `POST` | `/Api/V1/Student/OpenSessionRequests/Offers/{offerId}/Accept` |
| Reject | `POST` | `/Api/V1/Student/OpenSessionRequests/Offers/{offerId}/Reject` |
| View all reviews | `GET` | `/Api/V1/Student/Teachers/{teacherId}/Reviews` |

Owner/guardian only. List/detail return **all** offer statuses, including `Withdrawn`.

Accept/reject allowed only while the parent request is `Active` or `ReceivingOffers` (blocked for `Rejected` / `Cancelled` / `Expired` / `Paid` / etc.).

---

## Offer card → fields

| UI | JSON field | Notes |
|----|------------|--------|
| Avatar | `profilePictureUrl` | Absolute URL when set |
| Verified badge | `isVerified` | `true` when teacher `Status == Active` |
| Name | `teacherName` | |
| Rating | `ratingAverage` | 0–5 |
| Reviews count | `reviewsCount` | Approved reviews only |
| Price | `price` | Package **total** SAR (same as teacher submit) |
| Currency label | — | Show **`SAR`** (not “/ hr”) |
| Chat | `conversationId` | Open chat when non-null |
| View Offer | `id` | Navigate to Review Request |

Also on each list row: `sessionRequestId`, `teacherId`, `status`, `version`, `teacherNotes`, `expiresAt`, `createdAt`.

**Do not show:** years-of-experience chip (no API field).

---

## Review Request → fields

| UI | JSON field | Notes |
|----|------------|--------|
| Avatar / name / rating | same as card | |
| Subject tags | `subjectTags` | Request subject + up to 2 teacher subjects |
| Bio | `bio` | |
| TOTAL INVESTMENT | `price` | Package total |
| Status pill | `status` | Map offer status for display (e.g. `Pending`, `Accepted`) |
| Personalized note | `teacherNotes` | |
| Sessions row | `totalSessionsCount`, `sessionDurationMinutes` | Duration from first request session |
| Study content row | — | Optional: derive from parent OSR detail units/lessons if you already load the request; **no** dedicated materials field on the offer |
| Recent feedback | `recentReviews[]` | Top 2: `id`, `rating`, `feedback`, `studentDisplayName`, `createdAt` |
| View All | — | `GET /Student/Teachers/{teacherId}/Reviews` |
| Chat | `conversationId` | |

Detail also includes timestamps / `rejectionReason` / `subjectId` / `subjectName`.

**Do not show:** online presence dot (no API).

---

## Accept & Pay

1. `POST .../Offers/{offerId}/Accept`
2. Use response to open existing payment:

```json
{
  "offerId": 10,
  "enrollmentId": 55,
  "participantId": 90,
  "amountDue": 250.00,
  "paymentDeadline": "2026-08-03T12:00:00Z",
  "requestStatus": "PaymentPending"
}
```

Pay: `POST /Api/V1/Student/Payments/Participants` with `{ "data": { "participantId": 90 } }` (same enrollment payment path).

Reject: `POST .../Reject` with optional `{ "data": { "reason": "..." } }`.

---

## Explicit omissions

- Experience / years chip  
- Hourly rate (`price` is **total**)  
- Materials entity on the offer  
- Online indicator  

## Manual smoke checklist

1. Teacher: `POST /Teacher/Offers` on an open request → detail shows `myOfferId`.
2. Teacher UI: open request detail → offer card loads via `GET /Teacher/Offers/{myOfferId}` (notes visible).
3. Student: `GET .../OpenSessionRequests/{id}/Offers` → row includes avatar/rating/`isVerified`/`price`.
4. Student: `GET .../Offers/{offerId}` → bio, `subjectTags`, `sessionDurationMinutes`, `recentReviews`.
5. Student accept/reject only while request is `Active` | `ReceivingOffers`.

See also §6.7–6.8 in [`STUDENT-REQUEST-TEACHER.md`](STUDENT-REQUEST-TEACHER.md).
