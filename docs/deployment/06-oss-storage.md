# Alibaba OSS — auth & identity file storage

> **Deployment · OSS reference** · Buckets in SAU (Riyadh) `me-central-1`

Teacher documents and profile pictures are uploaded by **Qalam.MessagingApi** (RabbitMQ consumers) to Alibaba OSS. The API only queues messages.

## Buckets

| Environment | Bucket name | Public URL base (stored in SQL) |
|-------------|-------------|----------------------------------|
| Production | `auth-and-identities-certificates` | `https://auth-and-identities-certificates.oss-me-central-1.aliyuncs.com` |
| Staging / local dev | `auth-and-identities-certificates-staging` | `https://auth-and-identities-certificates-staging.oss-me-central-1.aliyuncs.com` |

**Region:** `me-central-1`  
**Upload endpoint (ECS, internal):** `https://oss-me-central-1-internal.aliyuncs.com`  
**Upload endpoint (local laptop):** `https://oss-me-central-1.aliyuncs.com`

## Console checklist (one-time)

For **each** bucket:

1. Region **SAU (Riyadh)** / `me-central-1`
2. ACL **Private**, object access **publicly accessible** (bucket policy)
3. **Versioning** enabled ([overview](https://www.alibabacloud.com/help/en/oss/user-guide/overview-78/))
4. **Lifecycle** — expire noncurrent versions after N days
5. **CORS** — prod/staging web origins if browsers load files directly from OSS
6. **OSS-HDFS** and **transfer acceleration** — leave disabled

**RAM user** (app runtime):

- Permissions on both buckets: `PutObject`, `GetObject`, `DeleteObject`, `ListObjects`
- Store keys in `.env.staging` / `.env.prod` as `OSS_ACCESS_KEY_ID` and `OSS_ACCESS_KEY_SECRET` (never commit)

## Environment variables

See [.env.staging.example](../../.env.staging.example) and [.env.prod.example](../../.env.prod.example).

| Variable | VPS staging/prod | Local Docker |
|----------|------------------|--------------|
| `OSS_BUCKET` | staging or prod bucket name | `auth-and-identities-certificates-staging` |
| `OSS_ENDPOINT` | `https://oss-me-central-1-internal.aliyuncs.com` | `https://oss-me-central-1.aliyuncs.com` |
| `OSS_USE_INTERNAL_ENDPOINT` | `true` | `false` |
| `OSS_PUBLIC_BASE_URL` | Matching bucket virtual-host URL | staging public base URL |

## Deploy after code change

Rebuild **messaging-api** (see [UPDATE_SERVER.md](../../UPDATE_SERVER.md)):

```bash
docker compose -f docker-compose.staging.yml -p qalam-staging --env-file .env.staging up -d --build messaging-api
```

## Local smoke test

1. Set `OSS_*` in repo root `.env` (staging bucket, **public** endpoint, `OSS_USE_INTERNAL_ENDPOINT=false`).
2. `docker compose up -d --build rabbitmq messaging-api qalam-api`
3. Upload test file:

```bash
curl -X POST "http://localhost:<api-port>/Api/Test/FileUploadTest/upload?teacherId=999&documentId=0" \
  -F "file=@/path/to/small.pdf"
```

4. `docker logs -f qalam-messaging-api` — expect OSS upload SUCCESS and HTTPS URL.
5. Confirm object in OSS console under `teachers/999/`.

## Migrate from Wasabi (if needed)

Only if `FilePath` / `ProfilePictureUrl` already contain `wasabisys.com`.

1. Copy objects with `rclone` or `ossutil` (preserve keys under `teachers/` and `profiles/`).
2. Run [scripts/oss-migrate-wasabi-urls.sql](../../scripts/oss-migrate-wasabi-urls.sql) on the target database (edit bucket URL if using staging).
3. Deploy MessagingApi with OSS env vars; revoke Wasabi keys.

`OssStorageService` still parses legacy Wasabi URLs for deletes during cutover.

## Restore a deleted file (versioning)

OSS console → bucket → **Previous Versions** → restore, or `ossutil revert`. See Alibaba versioning docs.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `AccessDenied` | RAM policy, wrong bucket, or keys not in env file |
| Timeout from laptop | Do not use `-internal` endpoint locally |
| Queue drains, no upload | MessagingApi down or OSS credentials empty |
| Browser 403 on file URL | Bucket public-read policy missing |
