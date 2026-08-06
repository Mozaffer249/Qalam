# Cursor Rules & Documentation Generator — Prompt

Copy the prompt below into Cursor Agent to generate project-specific Cursor rules and documentation.

---

## Prompt (copy from here)

```
Analyze this project and create Cursor rules plus documentation files.

## Goal

Set up persistent AI guidance so future agent sessions follow this project's conventions automatically.

## Deliverables

### 1. Cursor rules (`.cursor/rules/*.mdc`)

Create focused rule files with YAML frontmatter. Each rule must be:
- Under 50 lines of content
- One concern per file
- Actionable with concrete examples from THIS codebase

For monorepos, create separate rules per app/layer with path-specific globs:

| File | alwaysApply | globs | Purpose |
|------|-------------|-------|---------|
| `<project>-core.mdc` | `true` | — | Monorepo layout, principles, do/don't |
| Backend rule | `false` | backend source glob | Layering, naming, patterns |
| Frontend rule(s) | `false` | per-app glob | One rule per frontend app |
| Deployment rule | `false` | docker/env/scripts | Env vars, compose, deploy |
| Nested app rules | `false` | sub-app path | Link to or create detailed sub-rules |

Frontmatter format:
---
description: Brief description shown in rule picker
globs: **/*.ext
alwaysApply: false
---

### 2. Documentation (`docs/`)

- `docs/ARCHITECTURE.md` — system overview, layout, data flow, key files
- `docs/CONTRIBUTING.md` — setup, conventions, checklists per app/layer
- `docs/CURSOR-RULES-PROMPT.md` — this reusable prompt

Link to existing feature docs — do not duplicate API guides.

### 3. Root files

- `AGENTS.md` — agent quick reference (commands, key paths, doc links)
- Update `README.md` — add links to new docs (keep existing content)

## Process

1. Explore entry points, folder structure, 2–3 representative features per app
2. Identify real patterns used (not idealized)
3. Note legacy areas and submodule boundaries
4. Write rules with actual file paths from this repo
5. For monorepos: one always-apply core rule + scoped rules per app

## Constraints

- No secrets in rules/docs
- Match existing naming and import style
- Keep total rule content under 500 lines
- Submodule apps: document independent commit workflow

## Output

Summarize: rules created, docs created/updated, legacy patterns flagged.
```

---

## Customization by stack

| Layer | globs example |
|-------|----------------|
| .NET backend | `{Project.Api/**,Project.Core/**,Project.Data/**}` |
| Next.js admin | `apps/admin/**/*.{ts,tsx}` |
| Vite/React | `apps/teacher/**/*.{ts,tsx}` |
| Flutter | `apps/mobile/**/*.dart` |
| Docker/scripts | `{docker-compose*.yml,scripts/**,.env.example}` |

## What exists in this monorepo

### Root (C:\dev\Qalam)

| File | Scope |
|------|-------|
| `.cursor/rules/qalam-monorepo-core.mdc` | Always on |
| `.cursor/rules/dotnet-backend.mdc` | Qalam.* projects |
| `.cursor/rules/admin-frontend.mdc` | apps/admin |
| `.cursor/rules/teacher-frontend.mdc` | apps/teacher |
| `.cursor/rules/student-flutter.mdc` | apps/Qalam |
| `.cursor/rules/deployment-env.mdc` | Docker, env, scripts |
| `docs/ARCHITECTURE.md` | System overview |
| `docs/CONTRIBUTING.md` | Developer guide |
| `AGENTS.md` | Agent reference |

### Student app (apps/Qalam)

| File | Scope |
|------|-------|
| `apps/Qalam/.cursor/rules/qalam-core.mdc` | Always on (in app) |
| `apps/Qalam/.cursor/rules/flutter-architecture.mdc` | Dart files |
| `apps/Qalam/.cursor/rules/riverpod-patterns.mdc` | Providers |
| `apps/Qalam/.cursor/rules/localization.mdc` | Translations |
| `apps/Qalam/.cursor/rules/navigation.mdc` | GoRouter |
| `apps/Qalam/.cursor/rules/api-networking.mdc` | Repositories |

Open Cursor at the **monorepo root** (`C:\dev\Qalam`) for full-stack context. Rules activate based on the files you edit.
