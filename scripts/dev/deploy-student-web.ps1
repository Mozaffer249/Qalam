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
if ($Flavor -eq "staging") {
    $remoteName = "staging"
    $container = "qalam-staging-student"
    $composeFile = "docker-compose.staging.yml"
    $project = "qalam-staging"
    $envFile = ".env.staging"
    $healthUrl = "http://127.0.0.1:8094/"
}
else {
    $remoteName = "prod"
    $container = "qalam-student"
    $composeFile = "docker-compose.yml"
    $project = "qalam-prod"
    $envFile = ".env"
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

if (-not $SkipBuild) {
    Info "flutter build web --release -t lib/main_$Flavor.dart"
    Push-Location $appDir
    try {
        Invoke-Native "flutter pub get" { flutter pub get }
        Invoke-Native "flutter build web" { flutter build web --release -t "lib/main_$Flavor.dart" }
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
Invoke-Native "scp" { scp $archive "${VpsHost}:$remoteArchive" }
Ok "uploaded"

# Unpack beside the live directory, then swap, so the site is never half-written.
# Keeps the previous bundle at $prev for a quick manual rollback.
$remote = @(
    "set -e",
    "mkdir -p $remoteBase",
    "rm -rf $stage",
    "mkdir -p $stage",
    "tar -xzf $remoteArchive -C $stage",
    "rm -f $remoteArchive",
    "rm -rf $prev",
    "if [ -d $target ]; then mv $target $prev; fi",
    "mv $stage $target",
    "docker restart $container 2>/dev/null || (cd $repo && docker compose -f $composeFile -p $project --env-file $envFile up -d --no-deps qalam-student)",
    "sleep 2",
    "curl -sS -o /dev/null -w 'student HTTP %{http_code}\n' $healthUrl"
) -join "; "

Info "swapping bundle + restarting $container"
Invoke-Native "ssh" { ssh $VpsHost "bash -lc '$remote'" }

Remove-Item $archive -Force
Ok "deployed $Flavor student web to $target"
Write-Host "   rollback: ssh $VpsHost `"rm -rf $target && mv $prev $target && docker restart $container`"" -ForegroundColor Yellow
