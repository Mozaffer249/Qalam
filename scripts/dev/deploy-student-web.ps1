# =============================================================================
# deploy-student-web.ps1 - build the student Flutter web bundle LOCALLY and
# ship the static files to the VPS. Nothing is compiled on the server.
#
# Why: `docker compose up -d --build qalam-student` runs a release Flutter web
# compile on the VPS. That needs several GB of RAM plus every core, so it
# starves SQL Server / RabbitMQ / the APIs and the box looks hung. The bundle is
# just static files, so we build here and upload.
#
# Usage (from anywhere in the repo):
#   ./scripts/dev/deploy-student-web.ps1 staging
#   ./scripts/dev/deploy-student-web.ps1 production
#   ./scripts/dev/deploy-student-web.ps1 staging -SkipBuild      # reuse build/web
#   ./scripts/dev/deploy-student-web.ps1 staging -VpsHost root@1.2.3.4
#
# The API base URL is compiled in from lib/core/config/flavor_config.dart via
# the flavor entry point, so the flavor must match the target environment.
#
# Requires locally: flutter, ssh, scp, tar (all ship with Win10+ / Flutter SDK).
# Upload: Windows OpenSSH scp often resets mid-file (~few MB) to this VPS.
# We use classic scp (-O), SSH keepalives, and retries.
# =============================================================================
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("staging", "production")]
    [string]$Flavor,

    [string]$VpsHost = $(if ($env:QALAM_VPS_HOST) { $env:QALAM_VPS_HOST } else { "root@8.213.80.90" }),

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$appDir = Join-Path $root "apps/Qalam"
$webDir = Join-Path $appDir "build/web"

# Per-environment remote layout / container names.
# No -p for production: docker-compose.yml declares `name: qalam-platform`,
# which is the project every other prod command uses.
if ($Flavor -eq "staging") {
    $remoteName = "staging"
    $container = "qalam-staging-student"
    $composeArgs = "-f docker-compose.staging.yml -p qalam-staging --env-file .env.staging"
    $healthUrl = "http://127.0.0.1:8094/"
}
else {
    $remoteName = "prod"
    $container = "qalam-student"
    $composeArgs = "-f docker-compose.yml --env-file .env"
    $healthUrl = "http://127.0.0.1:8095/"
}

$repo = "/opt/qalam-backend/Qalam"
$remoteBase = "/opt/qalam-student-web"
$target = "$remoteBase/$remoteName"
$stage = "$target.new"
$prev = "$target.prev"
$archive = Join-Path $env:TEMP "qalam-student-$remoteName.tar.gz"
$remoteArchive = "/tmp/qalam-student-$remoteName.tar.gz"

function Info($msg) { Write-Host "-> $msg" -ForegroundColor Cyan }
function Ok($msg) { Write-Host "   $msg" -ForegroundColor Green }

function Invoke-Native($what, [scriptblock]$block) {
    & $block
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit $LASTEXITCODE)" }
}

$script:SshOpts = @(
    "-o", "ServerAliveInterval=15",
    "-o", "ServerAliveCountMax=12",
    "-o", "TCPKeepAlive=yes",
    "-o", "IPQoS=none"
)

function Invoke-UploadArchive {
    $dest = "${VpsHost}:$remoteArchive"
    $tries = 4
    for ($i = 1; $i -le $tries; $i++) {
        Info "uploading to $VpsHost (try $i/$tries)"
        # -O = original scp protocol. Default SFTP mode on Win OpenSSH dies with
        # "Couldn't send packet: Broken pipe" / "Connection reset" on this host.
        & scp -O @script:SshOpts $archive $dest
        if ($LASTEXITCODE -eq 0) { return }
        Write-Host "   scp exited $LASTEXITCODE, waiting then retry..." -ForegroundColor Yellow
        Start-Sleep -Seconds ([Math]::Min(20, 4 * $i))
    }
    throw @"
scp failed after $tries tries (Broken pipe / connection reset).
The Flutter build is already done — retry without rebuilding:
  ./scripts/dev/deploy-student-web.ps1 $Flavor -SkipBuild
If it keeps dying at a few MB, copy an SSH key to the VPS (password + Win scp is flaky on this link).
"@
}

if (-not $SkipBuild) {
    Info "flutter build web --release --pwa-strategy=none -t lib/main_$Flavor.dart"
    Push-Location $appDir
    try {
        Invoke-Native "flutter pub get" { flutter pub get }
        Invoke-Native "flutter build web" {
            flutter build web --release --pwa-strategy=none -t "lib/main_$Flavor.dart"
        }
    }
    finally { Pop-Location }
    Ok "bundle built"
}

if (-not (Test-Path (Join-Path $webDir "index.html"))) {
    throw "No bundle at $webDir (index.html missing). Run without -SkipBuild."
}

Info "packing $webDir"
if (Test-Path $archive) { Remove-Item $archive -Force }
Invoke-Native "tar" { tar -czf $archive -C $webDir . }
$sizeMb = [math]::Round((Get-Item $archive).Length / 1MB, 1)
Ok "$archive ($sizeMb MB)"

Info "uploading to $VpsHost"
Invoke-UploadArchive
Ok "uploaded"

# Unpack beside the live directory, then swap, so the site is never half-written.
# Keeps the previous bundle at $prev for a quick manual rollback.
$nginxConf = Join-Path $appDir "nginx.conf"
$nginxB64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($nginxConf))

$remoteLines = @(
    "set -e",
    "mkdir -p $remoteBase $repo/apps/Qalam",
    "echo $nginxB64 | base64 -d > $repo/apps/Qalam/nginx.conf",
    "rm -rf $stage",
    "mkdir -p $stage",
    "tar -xzf $remoteArchive -C $stage",
    "rm -f $remoteArchive",
    "rm -rf $prev",
    "if [ -d $target ]; then mv $target $prev; fi",
    "mv $stage $target",
    "cd $repo",
    "docker compose $composeArgs up -d --no-deps --force-recreate qalam-student",
    "sleep 2",
    "printf 'student HTTP %s\n' `"`$(curl -sS -o /dev/null -w '%{http_code}' $healthUrl)`""
)

# Shipped base64-encoded: ssh hands its command to a remote login shell, so raw
# quotes get re-parsed, and a literal CRLF script makes bash read `set -e\r`.
# Base64 is quote-free ASCII, so both problems disappear.
$remoteScript = ($remoteLines -join "`n") + "`n"
$encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($remoteScript))

Info "swapping bundle + restarting $container"
Invoke-Native "ssh" { ssh @script:SshOpts $VpsHost "echo $encoded | base64 -d | bash" }

Remove-Item $archive -Force
Ok "deployed $Flavor student web to $target"
Write-Host "   Hard-refresh the student site (or DevTools → Application → Clear site data) so old translation JSON / service worker are dropped." -ForegroundColor Yellow
Write-Host "   rollback: ssh $VpsHost `"rm -rf $target && mv $prev $target && docker restart $container`"" -ForegroundColor Yellow
