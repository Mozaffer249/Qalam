# LiveKit webhooks (local ngrok, staging, production)

LiveKit Cloud notifies Qalam when participants join or leave a live room. Official overview: [Webhooks & events](https://docs.livekit.io/intro/basics/rooms-participants-tracks/webhooks-events/).

API endpoint:

`POST /Api/V1/Live/Webhooks/LiveKit`

- Request `Content-Type`: `application/webhook+json` (we also accept `application/json` for Cloud “Send a test event”).
- `Authorization`: JWT signed with the webhook’s **Signing API key** (must match a key in that LiveKit project — same as compose `LIVEKIT_API_KEY` / `LIVEKIT_API_SECRET`).
- No user Bearer token. Nginx already proxies `/` to the API — **no extra location block**.

Events processed: `participant_joined`, `participant_left` (others are ignored). Rows are stored in `course.SessionLivePresenceEvents`.

**Also:** Enter room / Leave room via the teacher API (`Join` / `Leave` / `LiveToken`) appends presence events locally so the Attendance timeline works even when LiveKit Cloud webhooks are not reaching Docker (ngrok down). Webhooks remain the source of truth for student RTC join/leave when configured.

---

## Webhook URLs by environment

| Environment | LiveKit Cloud webhook URL |
|-------------|---------------------------|
| **Production** | `https://api.qalam.net.sa/Api/V1/Live/Webhooks/LiveKit` |
| **Staging** | `https://api-staging.qalam.net.sa/Api/V1/Live/Webhooks/LiveKit` |
| **Local (ngrok)** | `https://{subdomain}.ngrok-free.app/Api/V1/Live/Webhooks/LiveKit` (or `.ngrok-free.dev`) |

`PLATFORM_API_PUBLIC_BASE_URL` stays `http://localhost:8080` for local app links. Only the LiveKit webhook uses the ngrok HTTPS URL.

Prefer a **separate LiveKit Cloud project** (and keys) per environment.

---

## Windows ngrok (local /dev)

Follow the official [ngrok Windows get-started](https://dashboard.ngrok.com/get-started/setup/windows) (requires login).

1. Create/sign in to ngrok; open **Get Started → Setup → Windows**.
2. Install the agent (dashboard download, or `winget install ngrok.ngrok`).
3. Add your authtoken ([your-authtoken](https://dashboard.ngrok.com/get-started/your-authtoken)). **Quote it in PowerShell** so `$` is not treated as a variable:
   ```powershell
   ngrok config add-authtoken "YOUR_TOKEN_HERE"
   ngrok config check
   ```
4. Start the Qalam API on port **8080** (`scripts\dev\run-api.ps1` **or Docker** — `docker compose up` publishes API as `8080:80`).
5. Start the tunnel (leave the window open):
   ```powershell
   ngrok http 8080
   ```
6. Copy the **Forwarding** HTTPS URL (e.g. `https://abcd.ngrok-free.app`).
7. Apply migration `SessionLivePresenceEvents` if not already applied (`dotnet ef database update` against the same DB the API container uses).
8. Free ngrok URLs change on restart — update the LiveKit webhook URL (or use a reserved domain).

Inspector: `http://127.0.0.1:4040` shows POSTs from LiveKit.

---

## LiveKit Cloud webhook setup (checklist)

Use the **same** LiveKit Cloud project whose keys are in Docker `.env` (`LIVEKIT_API_KEY` / `LIVEKIT_API_SECRET`).

1. Sign in → **Settings** → [**Webhooks**](https://cloud.livekit.io/projects/p_/settings/webhooks).
2. **Create new webhook** (or edit existing):
   - **URL:** `https://{ngrok-or-api-host}/Api/V1/Live/Webhooks/LiveKit`
   - **Signing API key:** select the key that matches container `LIVEKIT_API_KEY` (mismatch → `Invalid webhook signature` / 401).
3. Ensure events include at least `participant_joined` and `participant_left` (unused types are ignored by Qalam).
4. **Actions → Send a test event** → pick `participant_joined`.
5. Watch API logs:

```powershell
docker compose logs -f qalam-api
```

Expect:

- `LiveKit webhook received: bodyLength=…, contentType=application/webhook+json`
- `LiveKit webhook verified: event=…` **or** a clear verify-fail hint about signing key mismatch
- `LiveKit webhook outcome: ok=…, statusCode=…`

Also confirm the POST appears in ngrok inspector (`http://127.0.0.1:4040`).

6. Real room test: Enter room with room `qalam-session-{scheduleId}` and identity `teacher-{id}` / `student-{id}`. App Join/Leave already writes timeline rows; Cloud webhooks add RTC-side events (may coexist on the append-only timeline).

**Notes**

- Logs like `Teacher N joined CourseSchedule X` are from the **Join/Leave API**, not Cloud webhooks. Cloud delivery shows `LiveKit webhook received`.
- Synthetic **Send a test event** payloads may not create DB rows (wrong room/identity); they still prove delivery and JWT verify.
- If you only see ngrok 404 / no API logs, the tunnel is not pointing at host port **8080**.

---

## LiveKit Cloud (staging / production)

1. Open the matching LiveKit project (keys = server `.env` `LIVEKIT_*`).
2. Settings → Webhooks → URL from the table above; **Signing API key** = that env’s `LIVEKIT_API_KEY`.
3. Events: at least `participant_joined`, `participant_left`.
4. Apply migration `SessionLivePresenceEvents` if not already applied.

---

## Smoke tests

Without a LiveKit JWT (proves the route is reachable; expect **401**):

```powershell
curl -i -X POST "https://api.qalam.net.sa/Api/V1/Live/Webhooks/LiveKit" `
  -H "Content-Type: application/webhook+json" -d "{}"
```

Same for staging / your ngrok HTTPS host.

---

## Related

- LiveKit docs: [Webhooks & events](https://docs.livekit.io/intro/basics/rooms-participants-tracks/webhooks-events/)
- Learning flow: [`../SESSIONS_LEARNING_FLOW.md`](../SESSIONS_LEARNING_FLOW.md) §5
- Nginx API vhosts: [`05-nginx-subdomains.md`](./05-nginx-subdomains.md) (webhook covered by `location /`)
