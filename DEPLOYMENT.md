# Deploy Qalam to Hostinger VPS

## Prerequisites

- Hostinger VPS with Docker installed
- A domain (e.g. `qalam.com`) pointed to your VPS IP
- Your repo pushed to GitHub (with `.env` gitignored)

---

## Step 1: SSH into VPS

```bash
ssh root@YOUR_VPS_IP
```

## Step 2: Install Nginx + Certbot

```bash
apt update && apt install -y nginx certbot python3-certbot-nginx
```

## Step 3: Clone your repo

```bash
cd /opt
git clone https://YOUR_TOKEN@github.com/YOUR_USER/Qalam.git
cd /opt/Qalam
```

## Step 4: Create `.env`

```bash
cp .env.example .env
nano .env
```

Fill in all values:

```env
# Database
DB_CONNECTION_STRING=Server=YOUR_DB_HOST;Database=YOUR_DB_NAME;User Id=YOUR_DB_USER;Password=YOUR_DB_PASS;Encrypt=False;MultipleActiveResultSets=True;TrustServerCertificate=True

# Encryption (MUST match what was used to encrypt existing data - DO NOT change)
ENCRYPTION_KEY=YOUR_ENCRYPTION_KEY

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_USER=qalam
RABBITMQ_PASS=STRONG_RABBIT_PASSWORD

# JWT
JWT_SECRET=YOUR_64_CHAR_PRODUCTION_SECRET_KEY_HERE
JWT_ISSUER=QalamProject
JWT_AUDIENCE=QalamProjectUsers

# CORS (comma-separated frontend URLs)
CORS_ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com

# Email (SMTP)
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_FROM_NAME=Qalam
EMAIL_FROM_EMAIL=noreply@yourdomain.com
EMAIL_USERNAME=noreply@yourdomain.com
EMAIL_PASSWORD=your-gmail-app-password

# Twilio SMS (optional)
TWILIO_ACCOUNT_SID=
TWILIO_AUTH_TOKEN=
TWILIO_FROM_NUMBER=

# Firebase Push (optional)
FIREBASE_KEY_PATH=
FIREBASE_PROJECT_ID=

# Wasabi Cloud Storage
WASABI_ACCESS_KEY=your-key
WASABI_SECRET_KEY=your-secret
WASABI_BUCKET=your-bucket
WASABI_REGION=ap-southeast-1
WASABI_SERVICE_URL=https://s3.ap-southeast-1.wasabisys.com
```

## Step 5: Build & Start

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Check status:

```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f qalam-api --tail 50
```

Wait until you see `Now listening on: http://[::]:80`

## Step 6: DNS Setup

In your domain registrar / Hostinger DNS panel:

| Type | Name  | Value         |
|------|-------|---------------|
| A    | `api` | `YOUR_VPS_IP` |

Wait for DNS propagation (can take up to 30 minutes).

## Step 7: Configure Nginx

```bash
nano /etc/nginx/sites-available/qalam
```

Paste:

```nginx
server {
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        client_max_body_size 50M;
    }
}
```

Enable:

```bash
ln -s /etc/nginx/sites-available/qalam /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx
```

## Step 8: SSL Certificate (Free)

```bash
certbot --nginx -d api.yourdomain.com
```

Choose **"redirect HTTP to HTTPS"**. Auto-renews every 90 days.

## Step 9: Verify

```bash
curl https://api.yourdomain.com/Api/V1/Messaging/Health
```

Expected response:

```json
{"status":"Healthy","service":"Messaging","timestamp":"..."}
```

---

## Ongoing Operations

### Update / Redeploy

```bash
cd /opt/Qalam
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

### View Logs

```bash
# All services
docker compose -f docker-compose.prod.yml logs -f --tail 100

# Specific service
docker compose -f docker-compose.prod.yml logs -f qalam-api --tail 100
docker compose -f docker-compose.prod.yml logs -f messaging-api --tail 100
docker compose -f docker-compose.prod.yml logs -f rabbitmq --tail 100
```

### Restart a Service

```bash
docker compose -f docker-compose.prod.yml restart qalam-api
```

### Stop Everything

```bash
docker compose -f docker-compose.prod.yml down
```

### Check Disk / Memory

```bash
docker stats --no-stream
df -h
```

---

## Architecture

```text
User (https://api.yourdomain.com)
        |
        v
   +---------+
   |  Nginx  |  port 443 (SSL) -> proxy to 127.0.0.1:8080
   +---------+
        |
        v
   +------------+         +----------------+         +------------+
   | qalam-api  | ------> | messaging-api  | ------> | rabbitmq   |
   | :8080      |         | (internal)     |         | (internal) |
   +------------+         +----------------+         +------------+
        |                        |
        v                        v
   +----------------------------------+
   |         SQL Server (remote)      |
   +----------------------------------+
```

## Security Checklist

- `.env` is gitignored and never committed
- `appsettings.json` has zero credentials (all empty strings)
- Encryption key read from environment variables, not source code
- Only port 8080 exposed, bound to `127.0.0.1` (only Nginx can reach it)
- RabbitMQ and MessagingApi have no exposed ports
- SSL via Let's Encrypt with auto-renewal
- RabbitMQ uses custom credentials (not default `guest/guest`)
