# Payment gateways

Card payments go through a pluggable `IPaymentGateway` registry: **Mock**, **Moyasar**, **PayTabs**, **HyperPay**, **Stripe**.

## Active provider (admin switch)

- **Env default:** `PAYMENT_PROVIDER` (compose → `PaymentSettings__Provider`).
- **Runtime override:** SuperAdmin → Settings → Payment gateway, stored as `SystemSettings` key `Payments.Gateway`.
- Takes effect on the **next** CreatePaymentIntent — no restart.
- Does **not** change confirm/refund for existing rows; those use `Payment.PaymentProvider`.
- API keys always stay in `.env`. Unconfigured providers show disabled in admin and cannot be activated.

## Webhook URL pattern

```
POST /Api/V1/Payments/Webhooks/{provider}
```

Legacy Moyasar path is kept: `/Api/V1/Payments/Webhooks/Moyasar`.

## Hosted return URL

Configure each hosted gateway's browser return URL to:

```
GET /Api/V1/Payments/Return/{provider}
```

Examples (staging):

| Variable | Suggested value |
|----------|-----------------|
| `MOYASAR_CALLBACK_URL` | `https://api-staging.qalam.net.sa/Api/V1/Payments/Return/Moyasar` |
| `PAYTABS_RETURN_URL` | `https://api-staging.qalam.net.sa/Api/V1/Payments/Return/PayTabs` |
| `HYPERPAY_SHOPPER_RESULT_URL` | `https://api-staging.qalam.net.sa/Api/V1/Payments/Return/HyperPay` |
| `STRIPE_SUCCESS_URL` | `https://api-staging.qalam.net.sa/Api/V1/Payments/Return/Stripe` |

The Flutter WebView intercepts this prefix (and falls back to common provider query markers).

| Provider | Auth | Notes |
|----------|------|-------|
| Moyasar | `secret_token` constant-time compare | Always re-fetch payment after auth |
| PayTabs | SHA-256 HMAC over sorted params | Server key |
| HyperPay | AES-256-GCM decrypt (IV + auth tag headers) | Activate webhook in dashboard after test |
| Stripe | `Stripe-Signature` HMAC-SHA256 | Timestamp tolerance |

## Environment

| Variable | Purpose |
|----------|---------|
| `PAYMENT_PROVIDER` | Env default: `Mock`, `Moyasar`, `PayTabs`, `HyperPay`, `Stripe` |
| `MOYASAR_PUBLISHABLE_KEY` / `MOYASAR_SECRET_KEY` / `MOYASAR_WEBHOOK_SECRET` / `MOYASAR_CALLBACK_URL` / `MOYASAR_CLIENT_MODE` / `MOYASAR_APPLE_PAY_MERCHANT_ID` / `MOYASAR_APPLE_PAY_LABEL` | Moyasar. `MOYASAR_CLIENT_MODE` is the **env default** (`HostedRedirect` or `NativeSdk`); SuperAdmin can override Moyasar presentation at runtime via Settings → Payment gateway → Moyasar checkout mode. |
| `PAYTABS_PROFILE_ID` / `PAYTABS_SERVER_KEY` / `PAYTABS_BASE_URL` / `PAYTABS_CALLBACK_URL` / `PAYTABS_RETURN_URL` | PayTabs |
| `HYPERPAY_ENTITY_ID` / `HYPERPAY_ACCESS_TOKEN` / `HYPERPAY_BASE_URL` / `HYPERPAY_WEBHOOK_KEY` / `HYPERPAY_SHOPPER_RESULT_URL` | HyperPay |
| `STRIPE_PUBLISHABLE_KEY` / `STRIPE_SECRET_KEY` / `STRIPE_WEBHOOK_SECRET` / `STRIPE_SUCCESS_URL` / `STRIPE_CANCEL_URL` | Stripe |

Compose maps these to `PaymentSettings__*` (see `docker-compose.yml` / `docker-compose.staging.yml`).

## Student API flow

1. `POST /Api/V1/Student/Payments/Intents` → provider-agnostic `PaymentIntentDto` (`provider`, `clientMode`, `givenId`, amount, optional `publishableApiKey` / `redirectUrl` / `clientSecret` / `applePayMerchantId`).
2. Flutter: `NativeSdk` → Moyasar method picker (Apple Pay / STC Pay / card); `HostedRedirect` → WebView on `redirectUrl` (Moyasar invoice URL when Moyasar is active in hosted mode).
3. `POST /Api/V1/Student/Payments/Confirm` `{ data: { givenId } }` → `ConfirmFromGatewayAsync` (shared with webhooks). For Moyasar hosted, `givenId` is the invoice id until confirm promotes it to the payment id.
4. Free-trial / Mock: `POST /Api/V1/Student/Payments/Participants`.
5. History: `GET /Api/V1/Student/Payments?pageNumber=1&pageSize=20` and receipt `GET /Api/V1/Student/Payments/{paymentId}`.

## Moyasar presentation (admin)

- SuperAdmin → Settings → Payment gateway → **Moyasar checkout mode**:
  - `HostedRedirect` (recommended): backend creates a Moyasar invoice; student pays in WebView.
  - `NativeSdk`: Flutter Moyasar widgets (card / Apple Pay / STC Pay).
- Stored in `SystemSettings` key `Payments.Gateway` as `moyasarClientMode` (same JSON as `activeProvider`).
- Env `MOYASAR_CLIENT_MODE` is only the seed/fallback when the DB field is empty.

## Intent reuse

- **NativeSdk:** reuses the open Pending payment row for the enrollment (same `givenId`) when the student re-enters checkout.
- **HostedRedirect:** cancels prior Pending rows (keeps `ProviderTransactionId` for late webhooks) and creates a fresh checkout session.
## Flip procedure

1. Put keys for the target gateway in VPS `.env` and restart once so `IsConfigured` becomes true.
2. Admin → Settings → Payment gateway → select provider → Save.
3. Smoke-test one enrollment checkout + refund.
4. Leave previous gateway keys in place until in-flight payments settle (refunds still need them).

## Moyasar webhook registration (example)

```sh
curl -X POST https://api.moyasar.com/v1/webhooks \
  -u "$MOYASAR_SECRET_KEY:" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://api-staging.qalam.net.sa/Api/V1/Payments/Webhooks/Moyasar",
    "http_method": "post",
    "shared_secret": "'"$MOYASAR_WEBHOOK_SECRET"'",
    "events": ["payment_paid", "payment_failed", "payment_refunded"]
  }'
```

## Staging checklist

1. Deploy with keys for at least one real gateway; leave `PAYMENT_PROVIDER=Mock` or flip via admin.
2. Register webhook(s) for the active gateway.
3. Pay → enrollment Active + schedules.
4. Decline → Failed, still payable.
5. Admin refund → provider refund id stored; uses **recorded** provider.
6. Replay webhook → idempotent confirm.
7. Flip active provider in admin; new intents use the new gateway; old pending confirms still work.

## Production

Flip to live keys after staging sign-off. Keep Mock as the local default. Never activate a gateway whose `IsConfigured` is false.