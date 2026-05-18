# Graphify — Complete Team Setup Guide

End-to-end guide for installing, configuring, and using Graphify as **shared team infrastructure** for this Flutter project. Covers tooling, project setup, team workflow, PR flow, and how to verify Claude actually reads from the graph.

---

## Table of Contents

1. [What Graphify Is and Why We Use It](#1-what-graphify-is-and-why-we-use-it)
2. [Pros and Cons](#2-pros-and-cons)
3. [Required Tools — Mac and Windows](#3-required-tools--mac-and-windows)
4. [Project Setup (One-Time, By One Developer)](#4-project-setup-one-time-by-one-developer)
5. [Team Onboarding (Every Other Developer)](#5-team-onboarding-every-other-developer)
6. [Daily Workflow](#6-daily-workflow)
7. [Working With the Team (Branches, Merges, Conflicts)](#7-working-with-the-team-branches-merges-conflicts)
8. [Creating a Pull Request](#8-creating-a-pull-request)
9. [How to Verify Claude Is Reading From the Graph](#9-how-to-verify-claude-is-reading-from-the-graph)
10. [Troubleshooting](#10-troubleshooting)
11. [Quick Reference](#11-quick-reference)

---

## 1. What Graphify Is and Why We Use It

Graphify scans the Flutter source code (`lib/`) and builds a **knowledge graph**: every Dart class, function, file, and import becomes a node; every reference becomes an edge. It then clusters related code into "communities", flags central files ("god nodes"), surfaces hidden cross-feature couplings, and produces three outputs:

- `graph.json` — raw structured data (4–5 MB on this codebase)
- `graph.html` — interactive visualization (open in any browser)
- `GRAPH_REPORT.md` — plain-language report

We use it to:
- Onboard new developers faster (visual map of the entire codebase in 30 s).
- Spot god classes and circular dependencies before they cause incidents.
- Let Claude answer architectural questions using **6.8× fewer tokens** than reading source files raw.
- Track architectural drift over time via `git log graphify-out/graph.json`.

---

## 2. Pros and Cons

### Pros

| Benefit | Detail |
|---|---|
| Fast onboarding | New developer opens `graph.html` after `git clone` and sees the whole architecture immediately. |
| Cheaper Claude queries | Claude reading 4 MB of graph context = ~42 K tokens. Reading the same context from source files = ~290 K tokens. |
| Catches hidden coupling | A widget under `features/A/` used by `features/B/` surfaces as a cross-community edge — usually a refactor signal. |
| Auto-maintained | Git hooks rebuild after every commit; no one has to remember. |
| Honest audit trail | Every edge is tagged `EXTRACTED` (from real imports/calls) or `INFERRED` (semantic) — no hallucinated relationships. |
| Architecture diff over time | `git log graphify-out/graph.json` shows how the codebase shape evolved per PR. |
| Works offline | After install, no API calls, no costs, no rate limits. AST extraction is deterministic. |

### Cons

| Drawback | How we mitigate it |
|---|---|
| Repo size grows | `graph.json` is ~4.5 MB per commit. Every code commit produces a new version. Over a year that's ~50–100 MB of history. → Acceptable for our use; revisit if it becomes painful. |
| Initial setup per developer | Each developer installs `uv` + `graphify` + registers a merge driver. ~5 minutes one-time. → Documented in section 5. |
| Hook adds 5–15 s to each commit | The post-commit hook re-extracts changed files. → Can skip with `git commit --no-verify` if you're in a hurry. |
| Semantic layer is gated on Claude permissions | Full LLM-based extraction (cross-file call patterns, similarity edges) requires Claude `Write` permission on `graphify-out/.graphify_chunk_*.json`. AST layer works without it. → AST-only graph is still very useful; semantic layer is a bonus. |
| Doesn't pick up doc/asset changes automatically | Hook only triggers on code changes. → Manual `graphify build lib --update` for doc updates. |
| Big PR diffs on `graph.json` | The file regenerates fully. → Solved by the `union-merge` driver (configured in step 4.2) which auto-resolves conflicts. |

---

## 3. Required Tools — Mac and Windows

| Tool | Purpose | Without it |
|---|---|---|
| `uv` | Python tool manager. Installs graphify in an isolated environment. | Can't install graphify cleanly. `pip install graphifyy` fails because the package is uv-only. |
| `graphify` | The CLI itself. | No graph builds, no hooks. |
| `git` | Version control. | Can't share the graph, no hooks, no merge driver. |
| Claude Code (optional) | Enables `/graphify query`, `/graphify path`, `/graphify explain` slash commands and the PreToolUse hook that forces Claude to consult the graph first. | You still get `graph.html` and `GRAPH_REPORT.md`, just no Claude integration. |

### 3.1 Install `uv`

**Mac:**
```bash
brew install uv
```
or, if you don't have Homebrew:
```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

**Windows (PowerShell as Administrator):**
```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```
or with Scoop:
```powershell
scoop install uv
```

Verify:
```bash
uv --version
```

### 3.2 Install `graphify`

Same command on every platform:
```bash
uv tool install --upgrade graphifyy
```

Then add `uv`'s tool directory to your shell PATH so the `graphify` command is found:

**Mac (zsh, default on macOS):**
```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

**Mac (bash):**
```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bash_profile
source ~/.bash_profile
```

**Windows (PowerShell):**
```powershell
# Add to your PowerShell profile permanently
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";$env:USERPROFILE\.local\bin", "User")
```
Then open a new PowerShell window.

Verify:
```bash
graphify --version
# Expected: graphify 0.8.5 (or newer)
```

### 3.3 `git`

Already required for our project — no action needed. Verify with `git --version`.

### 3.4 Claude Code (optional but recommended)

Download from <https://claude.com/claude-code>. After install, in this repo you can use `/graphify` slash commands and the `claude install` integration described in section 9.

---

## 4. Project Setup (One-Time, By One Developer)

> **This section has already been done for this repo** (commit `664fbcd3` on branch `graphify`). Keep this section for future projects or as a reference.

### 4.1 Build the initial graph

```bash
cd /path/to/thkel_app_customer
graphify build lib
```

`lib` is the scope — restrict to the Dart source. Without it, graphify would try to index the iOS/Android plugin folders (~3,200 files of generated code, mostly noise).

Output lands in `graphify-out/`. Takes ~30 s on this codebase.

### 4.2 Register the union-merge driver

This is what prevents merge conflicts on `graph.json` when two developers push changes in parallel.

Create `.gitattributes` in the repo root:
```
graphify-out/graph.json merge=graphify
```

This file **is committed** so every developer's git uses the driver.

### 4.3 Update `.gitignore`

Add this block to `.gitignore`:

```gitignore
# Graphify — per-developer state only (graph.json / graph.html / GRAPH_REPORT.md are shared)
graphify-out/manifest.json
graphify-out/cost.json
graphify-out/cache/
graphify-out/.graphify_*
```

**Why these specific paths?**
- `manifest.json` — file fingerprints with absolute paths and timestamps; breaks after `git clone`.
- `cost.json` — per-developer token usage tracker.
- `cache/` — semantic extraction cache; can be re-derived locally.
- `.graphify_*` — internal state files: `.graphify_python`, `.graphify_root`, temp chunk files. All contain machine-local absolute paths.

**Files NOT ignored (they will be committed):**
- `graphify-out/graph.json` — the actual graph
- `graphify-out/graph.html` — the visualization
- `graphify-out/GRAPH_REPORT.md` — the report

### 4.4 Register the merge driver in your local git config

> ⚠️ This step is **local-only** (each developer must do it on their own machine — git config isn't committed).

```bash
git config merge.graphify.name "graphify union-merge"
git config merge.graphify.driver "graphify merge-driver %O %A %B"
```

Verify:
```bash
git check-attr merge graphify-out/graph.json
# Expected: graphify-out/graph.json: merge: graphify
```

### 4.5 Install the git hooks

```bash
graphify hook install
```

This installs:
- `.git/hooks/post-commit` — rebuilds the graph after every commit (AST only, no LLM, no cost).
- `.git/hooks/post-checkout` — rebuilds when you switch branches.

These hooks are **not committed** (they live inside `.git/`). Each developer installs them on their own clone.

### 4.6 Commit and push

```bash
git add .gitignore .gitattributes \
        graphify-out/graph.json \
        graphify-out/graph.html \
        graphify-out/GRAPH_REPORT.md \
        docs/GRAPHIFY_AR.md
git commit -m "chore: enable shared graphify knowledge graph"
git push
```

---

## 5. Team Onboarding (Every Other Developer)

Run this on a fresh clone of the project. Total time: ~5 minutes.

```bash
# 1. Pull the latest from the branch that has the graphify setup
git checkout dev
git pull

# 2. Install uv (Mac shown; see section 3.1 for Windows)
brew install uv

# 3. Install graphify
uv tool install --upgrade graphifyy
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# 4. Register the merge driver in your LOCAL git config
git config merge.graphify.name "graphify union-merge"
git config merge.graphify.driver "graphify merge-driver %O %A %B"

# 5. Install the git hooks
graphify hook install
```

**Verify everything:**

```bash
graphify --version                              # 0.8.5 or newer
graphify hook status                            # both hooks: installed
git check-attr merge graphify-out/graph.json    # merge: graphify
git config --get merge.graphify.driver          # graphify merge-driver %O %A %B
open graphify-out/graph.html                    # opens in browser; you should see the graph
```

If all five checks pass, you're done.

---

## 6. Daily Workflow

The only difference from your normal Flutter workflow is: **commits become 5–15 s slower** because the hook regenerates the graph. Nothing else changes.

```bash
# Edit Dart code in lib/
git add lib/...
git commit -m "feat: add cargo refund flow"
# ▶ post-commit hook runs: re-extracts changed files, updates graph.json + GRAPH_REPORT.md

git status
# graphify-out/graph.json and GRAPH_REPORT.md show as modified (the hook just updated them).
```

**Amend the hook's changes into your commit:**
```bash
git add graphify-out/
git commit --amend --no-edit
```

> **Tip:** If you don't want to amend each time, you can `git push` separately — the graph update will be a follow-up commit. Either is fine; the union-merge driver handles parallel histories.

**Skipping the hook (rare):**
```bash
git commit --no-verify -m "..."   # Skips post-commit
```

**Updating after non-code changes (docs, assets):**
```bash
graphify build lib --update       # Re-extracts only changed files
```

---

## 7. Working With the Team (Branches, Merges, Conflicts)

### Pulling someone else's changes

```bash
git pull origin dev
```

If their changes touched `graph.json` and your local one also changed, git invokes the `graphify merge-driver` you registered in section 4.4. It union-merges the two JSON files automatically — **no conflict markers, no manual resolution.**

Result: your `graph.json` now contains nodes/edges from both versions. The next commit will normalize it.

### Switching branches

```bash
git checkout feat/some-feature
# ▶ post-checkout hook runs: rebuilds graph for this branch's code
```

### Rebasing onto `dev`

```bash
git fetch origin
git rebase origin/dev
```

Hooks are **skipped during rebase** (graphify intentionally exits early to avoid blocking `--continue`). Once the rebase finishes, the next regular commit will refresh the graph.

### Resolving a real merge conflict

The merge driver handles `graph.json` automatically. For Dart files, resolve conflicts normally, then:

```bash
git commit
# ▶ Hook re-extracts the final code state. Graph is consistent again.
```

---

## 8. Creating a Pull Request

```bash
git checkout -b feat/my-feature
# ... write code, commit, push ...
git push -u origin feat/my-feature
gh pr create --base dev --title "feat: my feature" --body "..."
```

### What the reviewer sees

- Your code changes (normal Dart diff).
- `graph.json` / `graph.html` / `GRAPH_REPORT.md` diffs reflecting the new/changed nodes and edges.

The reviewer can:
- **Open `graph.html` from your branch** in a browser to visually inspect what changed structurally.
- **Read the updated `GRAPH_REPORT.md`** to see if your changes introduced new god nodes or hurt community cohesion.
- **Spot accidental coupling** — e.g. a new edge crossing from `features/cargos` into `features/auth` might warrant a question.

### PR review tip

Add this to your PR description:

> **Graphify report:** see updated `graphify-out/GRAPH_REPORT.md`. Notable changes:
> - New community: `Refund Flow` (12 nodes)
> - `core/widgets/app_text.dart` god-node count went from 80 → 82

---

## 9. How to Verify Claude Is Reading From the Graph

**This is the critical question.** Claude has access to both the source code *and* the graph. By default it will read source files. To make Claude actually use the graph, do one or more of the following.

### Method 1: Use `/graphify` slash commands explicitly (always works)

These commands hit `graph.json` directly — Claude has no choice:

```
/graphify query "How does authentication work in this app?"
/graphify path "AuthRepository" "ApiEndPoints"
/graphify explain "AppDecoration"
```

**How you'll know it worked:**
- Claude's response cites graph artifacts: community names ("the AuthRepository sits in the *Auth & Booking Domain* community"), edge types ("via an `implements` edge"), and god-node mentions.
- Token usage is far smaller than a typical "read all files" response.
- You can see Claude calls a `graphify query` tool internally (visible in tool calls).

### Method 2: Install the PreToolUse hook (strongest guarantee)

```bash
graphify claude install
```

This:
1. Adds a `## graphify` section to `CLAUDE.md` instructing Claude to check the graph first.
2. **Installs a PreToolUse hook** in `.claude/settings.json` that intercepts Read/Grep calls and routes architectural questions through `graph.json`.

Now even a casual question like *"Where's the cargo notification handler?"* triggers a graph lookup before Claude opens any source file.

Verify it's installed:
```bash
cat CLAUDE.md | grep -A 5 "graphify"
cat .claude/settings.json | grep -A 5 "PreToolUse"
```

Uninstall any time:
```bash
graphify claude uninstall
```

### Method 3: Visual signals in Claude's responses

When Claude consults the graph, the response usually contains one or more of these patterns:

- "This file lives in the **[Community Name]** community."
- "It's a god node with **N edges**."
- "The shortest path from X to Y goes through Z."
- "Cohesion score: 0.42."
- "Cross-community edge to **[Other Community]**."

If Claude's response reads like a normal code explanation with no graph terminology, it likely just read the file directly.

### Method 4: Check graph access via file mtime

Before asking Claude an architectural question:
```bash
stat -f "%Sm" graphify-out/graph.json     # Mac
# Windows: (Get-Item graphify-out/graph.json).LastAccessTime
```

Note the access time. Ask Claude the question. Then re-check the access time — if it changed, the graph was read.

> **Reality check:** Method 1 is the only one with a guarantee that's free of ambiguity. For the rest, the PreToolUse hook (Method 2) is the strongest practical guarantee.

---

## 10. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `graphify: command not found` | PATH not configured | `export PATH="$HOME/.local/bin:$PATH"` and add to shell profile |
| `No matching distribution for graphifyy` | Using pip instead of uv | Use `uv tool install graphifyy` |
| Hook doesn't run on commit | Hook file missing or not executable | `graphify hook status`; reinstall with `graphify hook install` |
| Merge conflict on `graph.json` | Merge driver not registered locally | Re-run the `git config merge.graphify.*` commands from section 4.4 |
| Commit feels slow | Hook is rebuilding the graph | Normal: 5–15 s on this codebase. Use `--no-verify` for emergencies. |
| `graph.html` won't open | File path issue | `open graphify-out/graph.html` (Mac) / `start graphify-out/graph.html` (Windows) |
| Claude ignores the graph | No PreToolUse hook installed | Run `graphify claude install` (section 9, Method 2) |
| Graph is stale after pulling | Hook didn't run on others' commits | `graphify build lib --update` |
| Repo size growing | Each commit adds ~4.5 MB to `graph.json` history | Acceptable for our project; if it ever becomes painful, switch to a per-developer setup (revert commit `664fbcd3`) |

---

## 11. Quick Reference

### One-time per developer
```bash
brew install uv                                                     # Mac, see section 3 for Windows
uv tool install --upgrade graphifyy
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc
git config merge.graphify.name "graphify union-merge"
git config merge.graphify.driver "graphify merge-driver %O %A %B"
graphify hook install
graphify claude install     # optional, makes Claude consult the graph automatically
```

### Daily commands
```bash
graphify build lib                  # full rebuild
graphify build lib --update         # incremental
graphify hook status                # check hooks
/graphify query "..."               # in Claude Code: ask about architecture
/graphify path A B                  # in Claude Code: shortest path between two nodes
open graphify-out/graph.html        # visual exploration
```

### Files you commit
```
.gitignore                          (modified, includes Graphify block)
.gitattributes                      (1-line union-merge directive)
graphify-out/graph.json             (shared graph data)
graphify-out/graph.html             (shared viewer)
graphify-out/GRAPH_REPORT.md        (shared report)
docs/GRAPHIFY_AR.md                 (this document)
```

### Files you don't commit (covered by `.gitignore`)
```
graphify-out/manifest.json
graphify-out/cost.json
graphify-out/cache/
graphify-out/.graphify_*
```

---

**References:**
- Official source: <https://github.com/safishamsi/graphify>
- Skill manual: `~/.claude/skills/graphify/SKILL.md`
- This project's first build: 652 Dart files → 4,385 nodes / 7,216 edges in ~30 s.
