# =============================================================================
# push-submodules.ps1 - commit + push the admin/teacher submodules, then bump
# the pointer in the main Qalam repo. One command for the whole flow.
#
# Why: editing apps/admin or apps/teacher creates commits that live ONLY inside
# the submodule. If you push the main repo without first pushing the submodule,
# the VPS `git pull --recurse-submodules` fails ("reference is not a tree"),
# because the commit the superproject points at doesn't exist on the remote yet.
# This script pushes the submodules FIRST, then the superproject pointer.
#
# Usage (from anywhere in the repo):
#   ./scripts/dev/push-submodules.ps1 "your commit message"
#   ./scripts/dev/push-submodules.ps1 "fix teacher form" -Branch main
#   ./scripts/dev/push-submodules.ps1 "wip" -SkipSuper   # push submodules only
#
# Works with either workflow:
#   a) Let the script commit + push everything (pass a real -Message).
#   b) Commit each repo yourself in the Source Control UI, then run the script
#      to push them all in the correct order. Already-committed repos are just
#      pushed; the -Message is only used if something is still uncommitted.
#
# Parameters:
#   -Message    Commit message used for submodule + superproject commits (required).
#   -Branch     Branch to push to in every repo (default: main).
#   -SkipSuper  Only commit/push the submodules; do NOT touch the main repo.
# =============================================================================
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Message,
    [string]$Branch = "main",
    [switch]$SkipSuper
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$submodules = @("apps/admin", "apps/teacher")

function Info($msg) { Write-Host "-> $msg" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "   $msg" -ForegroundColor Green }
function Warn($msg) { Write-Host "   $msg" -ForegroundColor Yellow }

# Run a git command against a specific repo path; throw on failure.
function Git-In($path, [string[]]$GitArgs) {
    & git -C $path @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed in $path (exit $LASTEXITCODE)"
    }
}

# True when the working tree at $path has staged or unstaged changes.
function Has-Changes($path) {
    & git -C $path add -A
    & git -C $path diff --cached --quiet
    return ($LASTEXITCODE -ne 0)
}

foreach ($sub in $submodules) {
    $path = Join-Path $root $sub
    if (-not (Test-Path (Join-Path $path ".git"))) {
        Warn "$sub not initialized (run: git submodule update --init --recursive) - skipping"
        continue
    }

    Info "Submodule $sub"

    if (Has-Changes $path) {
        # Submodules are usually checked out detached; anchor a real branch so
        # the commit lands on $Branch, then commit the staged changes.
        $current = (& git -C $path rev-parse --abbrev-ref HEAD).Trim()
        if ($current -eq "HEAD") {
            Git-In $path @("checkout", "-B", $Branch)
            Ok "was detached; moved onto $Branch"
        }
        Git-In $path @("commit", "-m", $Message)
        Ok "committed changes"
    }
    else {
        Ok "no local changes"
    }

    # Push whatever HEAD points at to the remote branch (no-op if already there).
    Git-In $path @("push", "origin", "HEAD:$Branch")
    Ok "pushed to origin/$Branch"
}

if ($SkipSuper) {
    Warn "Skipping superproject pointer update (-SkipSuper)."
    return
}

Info "Main repo (superproject)"
& git -C $root add -- $submodules
& git -C $root diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    Git-In $root @("commit", "-m", $Message)
    Ok "committed submodule pointer bump"
}
else {
    Ok "no new pointer changes to stage (already committed?)"
}

# Always push - covers the case where you committed via the Source Control UI
# and only want the script to push. No-op ("Everything up-to-date") if nothing
# is pending.
Git-In $root @("push", "origin", "HEAD:$Branch")
Ok "pushed superproject to origin/$Branch"

Info "Done. On the VPS run: sudo bash scripts/vps/deploy.sh"
