# Command-Line Deployment Guide for RunASP.NET

## 🚀 Quick Deployment (Recommended)

Since Web Deploy (msdeploy) is Windows-only, here are your command-line options:

---

## Option 1: Direct Upload via Control Panel (Easiest)

1. Open your browser and log in to RunASP.NET control panel
2. Navigate to File Manager
3. Run this command to copy the ZIP to desktop for easy access:
```bash
cp qalam-deployment.zip ~/Desktop/
```
4. Upload `qalam-deployment.zip` and extract it

---

## Option 2: FTP Deployment (If FTP credentials are available)

**First, verify your FTP credentials from RunASP.NET control panel**, then install `lftp`:

```bash
# Install lftp (faster than curl for bulk uploads)
brew install lftp

# Deploy
lftp -u site49539,YOUR_FTP_PASSWORD site49539.siteasp.net <<EOF
set ftp:ssl-allow no
mirror -R publish /httpdocs
bye
EOF
```

---

## Option 3: Using Docker + Web Deploy (Complex but automated)

If you have Docker installed:

```bash
./deploy-webdeploy.sh
```

This runs msdeploy inside a Windows container.

---

## Option 4: Manual curl Upload (Slower but works)

If you have valid FTP credentials:

```bash
cd publish

# Upload each file individually
for file in *; do
    curl -T "$file" \
         --user "site49539:YOUR_PASSWORD" \
         "ftp://site49539.siteasp.net/httpdocs/$file"
done
```

---

## 📝 Get Your Actual FTP Credentials

The credentials in the publish settings file might be Web Deploy-specific. To get FTP credentials:

1. Log in to RunASP.NET control panel
2. Go to: **Websites** → **Your Site** → **FTP Accounts**
3. Note the FTP host, username, and password
4. Use those in the commands above

---

## 🔍 Verify Deployment

After deployment:

```bash
# Check if API is responding
curl -I http://qalam.runasp.net/

# Test Swagger endpoint
curl http://qalam.runasp.net/swagger/index.html
```

---

## ⚡ Fastest Method (What I Recommend)

1. **Copy ZIP to accessible location:**
   ```bash
   cp qalam-deployment.zip ~/Desktop/
   ```

2. **Upload via Control Panel:**
   - RunASP.NET Control Panel → File Manager
   - Upload `qalam-deployment.zip`
   - Extract to root directory
   - Delete old files if needed

This is faster than FTP and more reliable than trying to run Windows tools on macOS.

---

## 🆘 If You Need Automated Deployment

Consider setting up:
- GitHub Actions (deploy on push)
- GitLab CI/CD
- Azure DevOps Pipeline

These can run on Windows agents with native msdeploy support.
