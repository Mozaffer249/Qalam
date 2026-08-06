# OSR notification settings (admin)

SuperAdmin controls outbound channels when teachers are matched or directly targeted on open session requests.

Inbox **target rows are always created**. Email / SMS / push only fire when the corresponding flag is enabled.

## Endpoints

| Method | Path |
|--------|------|
| GET | `/Api/V1/Admin/SystemSettings/OsrNotifications` |
| PUT | `/Api/V1/Admin/SystemSettings/OsrNotifications` |

Auth: **SuperAdmin** role.

### Body / response

```json
{
  "emailEnabled": true,
  "smsEnabled": false,
  "pushEnabled": false
}
```

Defaults (seeded as `OSR.Notifications` in `common.SystemSettings`): email **on**, SMS/push **off**.

## Channel behavior

| Channel | When enabled |
|---------|----------------|
| Email | Queues email to the teacher’s user email |
| SMS | Queues SMS when `AspNetUsers.PhoneNumber` is set; skips teachers without a phone |
| Push | Toggle stored; **no-op until device tokens are registered** (no token table yet) |

## Example

```http
PUT /Api/V1/Admin/SystemSettings/OsrNotifications
Authorization: Bearer {superAdminToken}
Content-Type: application/json

{
  "emailEnabled": true,
  "smsEnabled": true,
  "pushEnabled": false
}
```

Related: rematch when a teacher adds/reactivates a subject also respects these toggles.
