# Object storage — Alibaba OSS ⇄ Wasabi

> **Deployment · Object storage** · Dual-provider switch via `STORAGE_PROVIDER`

Qalam stores teacher documents, profile pictures, course images, content library files, open-session attachments, and complaint attachments in object storage. **Qalam.Api** queues uploads and precomputes public URLs; **Qalam.MessagingApi** performs Put/Delete.

## Provider switch

| `STORAGE_PROVIDER` | Uploads go to | Public URL host |
|--------------------|---------------|-----------------|
| `alibaba` (default) | Alibaba OSS | `*.oss-me-central-1.aliyuncs.com` |
| `wasabi` | Wasabi Hot Cloud Storage | `*.s3.{region}.wasabisys.com` |

Both `OSS_*` and `WASABI_*` stay configured so you can flip either direction with an env change + redeploy of `qalam-api` and `messaging-api`. **Deletes route by URL host** (`wasabisys.com` → Wasabi, `aliyuncs.com` → Alibaba), so leftover objects on the previous provider remain deletable after a cutover.

## Buckets (staging)

Same physical names on both providers (keys preserved by rclone):

| Logical | Bucket name | Alibaba public base | Wasabi public base |
|---------|-------------|---------------------|--------------------|
| Identities | `auth-and-identities-certificates-staging` | `https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com` | `https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com` |
| Learning | `qalam-content-stg` | `https://qalam-content-stg.oss-me-central-1.aliyuncs.com` | `https://qalam-content-stg.s3.ap-southeast-1.wasabisys.com` |

**Alibaba region:** `me-central-1` (SAU Riyadh)  
**Wasabi region (default):** `ap-southeast-1` — change `WASABI_REGION` / `WASABI_ENDPOINT` / public bases if buckets live elsewhere ([service URLs](https://docs.wasabi.com/docs/what-are-the-service-urls-for-wasabis-different-storage-regions)).

Production keeps `STORAGE_PROVIDER=alibaba` until a separate cutover. Prod bucket names differ (`auth-and-identities-certificates`, `qalam-content-prod`); reuse the same switch pattern.

## Wasabi console checklist (one-time)

For **each** staging bucket:

1. Region matches `WASABI_REGION`
2. Versioning enabled
3. Bucket is private by default — add a bucket policy that allows anonymous `s3:GetObject` if browsers/apps load files via the public URL (same model as current Alibaba public-read objects)
4. CORS — staging web origins if browsers load files directly
5. Note: Wasabi bills a **90-day minimum** storage duration; churny deletes still incur cost

**Access keys:** create a Wasabi sub-user with Put/Get/Delete/List on both buckets. Store as `WASABI_ACCESS_KEY_ID` / `WASABI_ACCESS_KEY_SECRET` (never commit). Alibaba ECS RAM (`OSS_ECS_ROLE_NAME`) applies only to the Alibaba path.

## Environment variables

See [.env.staging.example](../../.env.staging.example) and [.env.example](../../.env.example).

| Variable | Role |
|----------|------|
| `STORAGE_PROVIDER` | `alibaba` \| `wasabi` |
| `OSS_*` | Alibaba credentials, endpoints, both bucket public bases |
| `WASABI_*` | Wasabi credentials, endpoint, both bucket public bases |

Wire both providers into **messaging-api** (uploads/deletes) and **qalam-api** (URL precompute) via compose.

## Staging cutover (Alibaba → Wasabi)

1. **Copy objects** (preserve keys). Configure rclone remotes for Alibaba (`provider=Alibaba`) and Wasabi (`provider=Wasabi`) per [Wasabi rclone docs](https://docs.wasabi.com/v1/docs/how-do-i-use-rclone-with-wasabi):

```sh
rclone sync alibaba:auth-and-identities-certificates-staging wasabi:auth-and-identities-certificates-staging
rclone sync alibaba:qalam-content-stg wasabi:qalam-content-stg
```

Optional managed alternative for large / prod windows: [Wasabi Cloud Sync Manager](https://docs.wasabi.com/v1/docs/wcsm-wasabi-cloud-sync-manager).

2. Set `WASABI_*` keys and `STORAGE_PROVIDER=wasabi` in `.env.staging`. Redeploy:

```sh
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build messaging-api qalam-api
```

3. Final incremental `rclone sync` for uploads that landed on Alibaba during the redeploy.

4. Run [scripts/storage-staging-switch-to-wasabi.sql](../../scripts/storage-staging-switch-to-wasabi.sql) — review inventory counts, then `COMMIT`.

5. Smoke: upload teacher document, profile picture, course image, OSR attachment, complaint attachment; confirm Wasabi console + `wasabisys.com` URLs in SQL; delete one Wasabi object and one leftover Alibaba URL (host-based delete routing).

6. Keep Alibaba credentials valid until production also migrates.

### Rollback

1. `STORAGE_PROVIDER=alibaba` + redeploy `messaging-api` and `qalam-api`.
2. Run [scripts/storage-staging-rollback-to-oss.sql](../../scripts/storage-staging-rollback-to-oss.sql) if DB URLs must point at Alibaba again.

## Local smoke test

1. Set `STORAGE_PROVIDER` and the matching provider credentials in repo-root `.env`.
2. `docker compose up -d --build rabbitmq messaging-api qalam-api`
3. Upload via API test endpoint or teacher/student flows.
4. `docker logs -f qalam-messaging-api` — expect upload SUCCESS to the active provider and the matching HTTPS URL host.

## Legacy SQL (Wasabi → Alibaba)

[scripts/oss-migrate-wasabi-urls.sql](../../scripts/oss-migrate-wasabi-urls.sql) is the older one-way rewrite (path-style Wasabi → Alibaba). Prefer the staging switch/rollback scripts above for the dual-provider cutover.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| URL host does not match where the file landed | `STORAGE_PROVIDER` must match on **both** `qalam-api` and `messaging-api` |
| `AccessDenied` on Wasabi | Keys, bucket policy, or wrong region endpoint |
| Delete fails after cutover | Keep both providers' credentials; deletes route by URL host |
| Timeout from laptop to Alibaba | Do not use `-internal` OSS endpoint locally |
| Browser 403 on file URL | Public-read GetObject policy missing on the active bucket |
