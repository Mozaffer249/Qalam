# Student OSR chat (hybrid model)

> **Audience:** Student app / Flutter frontend  
> **Base path:** `/Api/V1/Conversations`  
> **Auth:** JWT as request owner (`RequestedByUserId` — student or guardian who submitted)

Related: [Student request teacher](STUDENT-REQUEST-TEACHER.md) · [S2 flow & endpoints](S2-FLOW-AND-ENDPOINTS.md)

---

## Model

| Request type | Detected by | Conversation key | Pre-offer chat |
|--------------|-------------|------------------|----------------|
| **Targeted** | `targetedTeacherId` set | `(sessionRequestId, teacherId)` — one thread | **Yes** (`offerId` may be `0`) |
| **Broadcast** | `targetedTeacherId` null | `sessionOfferId` — one thread **per offer** | **No** — chat only after an offer exists |

- **Targeted** withdraw + re-offer → **same** conversation; `offerId` pointer updates.
- **Broadcast** withdraw + re-offer → **new** conversation for the new offer.
- DTO includes `isOfferScoped` (`true` = broadcast / offer-keyed).

Enrollment chat (`/EnrollmentConversations`) is separate and out of scope here.

---

## When to open

| Mode | Open when | Entry |
|------|-----------|--------|
| Targeted | After publish (clarification OK before any offer) | `GET /Conversations/by-request/{requestId}/teacher/{teacherId}` |
| Broadcast | Student has an offer from that teacher | `GET /Conversations/by-offer/{offerId}` |

Calling `by-request` on a **broadcast** request returns **400** (`BROADCAST_USE_BY_OFFER`).

`by-offer` also works for **targeted** (resolves to the single request-scoped thread and points at that offer).

---

## Endpoints

| Method | Path | Body / query | Notes |
|--------|------|--------------|--------|
| GET | `/Conversations/by-request/{requestId}/teacher/{teacherId}` | — | Targeted only |
| GET | `/Conversations/by-offer/{offerId}` | — | Broadcast (required); targeted OK |
| GET | `/Conversations/{conversationId}/messages` | `cursor?`, `take=50`, `direction=older\|newer` | Cursor = ISO-8601 `sentAt` |
| POST | `/Conversations/{conversationId}/messages` | `{ "content": "..." }` | Max ~4000 chars |
| POST | `/Conversations/{conversationId}/read` | `{ "upToMessageId": 123 }` optional | Marks read for caller |

### Header DTO (get-or-create)

```json
{
  "conversationId": 412,
  "offerId": 892,
  "isOfferScoped": false,
  "participants": [
    { "userId": 88, "displayName": "…", "role": "Student" },
    { "userId": 17, "displayName": "…", "role": "Teacher" }
  ],
  "lastMessageAt": "2026-05-17T14:30:00Z",
  "unreadCount": 3
}
```

`offerId: 0` means no offer linked yet (targeted preliminary chat only).

### Message DTO

`id`, `type` (`Text` / `System` / …), `senderUserId`, `senderDisplayName`, `senderRole` (`Teacher` \| `Student` \| null for system), `content`, `sentAt`.

---

## Client tips

- Poll messages while the chat screen is open (no SignalR in v1).
- Sort/display ascending by `sentAt` if the API returns `direction=older` (newest-first page).
- Align bubbles with `senderRole` or compare `senderUserId` to the current user.
- No attachments in v1.
