# Genesis AI — Plan 4c Feature Guide

## Overview

Plan 4c wires Genesis AI to GitHub. Every approved artefact from every pipeline stage is automatically committed to the feature repo by `genesis-ai[bot]`. This creates a complete, immutable audit trail in Git — from first requirement to final sign-off.

---

## Feature 1 — P00 Project Settings

**Where:** Project Detail page → Settings tab (last tab)

**What it does:** Stores GitHub configuration and project metadata that flows into every pipeline stage automatically. No stage needs to ask for this information again.

**How to use:**

1. Open any project → click the Settings tab
2. **GitHub Configuration section:**
   - Paste the GitHub API repo URL (e.g. `https://github.com/emisgroup/emis-x-document-manager`)
   - Paste the GitHub App repo URL (e.g. `https://github.com/emisgroup/emis-x-document-manager-app`)
   - Click "Save GitHub Configuration"
   - On first save, `genesis-ai[bot]` automatically creates the `.genesis/` folder structure in the repo (see Feature 2)
3. **P00 Configuration section:**
   - Select Release Type (EMIS Web or EMIS-X)
   - Set Assurance Required, CSO/IG/Security role flags, Medical Device flag
   - Add Pilot/Deployment Process notes
   - Click "Save P00 Configuration"
   - A `PROJECT.md` is committed to `.genesis/project/` in the feature repo

**What happens in the background:**
- GitHub repo URLs and installation ID are stored encrypted in the DB
- On first GitHub config save, `ScaffoldGenesisStructureAsync` fires automatically (best-effort, non-blocking)
- P00 fields are committed as `PROJECT.md` to `.genesis/project/` via `genesis-ai[bot]`

**Push Status badge:**
- Below the P00 form, a status indicator polls every 60 seconds
- If any artefact failed to push to GitHub, shows: "N artefact(s) could not be pushed to GitHub. Check your GitHub configuration."
- Green when all pushes are healthy

---

## Feature 2 — Automatic .genesis/ Scaffold

**Triggered by:** Saving GitHub configuration for the first time on a project

**What it creates in the feature repo:**

```
.genesis/
  requirements/.gitkeep
  architecture/.gitkeep
  clinical-safety/.gitkeep
  ig/.gitkeep
  security/.gitkeep
  prototype/.gitkeep
  session-close/.gitkeep
  project/.gitkeep
  project/PROJECT.md        ← P00 fields rendered as markdown
  CODEOWNERS                ← team-based prompt ownership
  .gitkeep                  ← sentinel (pushed last, idempotency check)
```

**Idempotency:** If `.genesis/.gitkeep` already exists, scaffold skips silently. Safe to re-trigger.

**Commit message format:**
```
chore(genesis): scaffold .genesis/ structure

Provisioned-By: genesis-ai[bot]
Triggered-By: {user ERN}
Project-ID: {uuid}
Genesis-AI-Version: 1.0.0.0
```

**CODEOWNERS content:**
```
src/Genesis.AI.Infrastructure/Prompts/Pipeline06* @emisgroup/clinical-safety-owners
src/Genesis.AI.Infrastructure/Prompts/Pipeline07* @emisgroup/ig-owners
src/Genesis.AI.Infrastructure/Prompts/Pipeline08* @emisgroup/security-owners
```
Team membership managed in EMIS-X Auth / IAM platform.

---

## Feature 3 — Automatic Artefact Push on Approval

**Triggered by:** Any artefact approval in any pipeline stage

**What happens:**
1. User approves a REQ file, ARCH document, DCB0129 hazard log, etc.
2. Artefact is saved to S3 and DB as normal
3. `ArtefactPublishedDomainEvent` fires
4. `GitHubArtefactPushService` reads the artefact from S3
5. `genesis-ai[bot]` commits it to `.genesis/` in the feature repo

**Path mapping:**

| Genesis AI path | GitHub path |
|----------------|-------------|
| `requirements/REQ-001.md` | `.genesis/requirements/REQ-001.md` |
| `requirements/CHANGE-001.md` | `.genesis/requirements/CHANGE-001.md` |
| `architecture/ARCH-001.md` | `.genesis/architecture/ARCH-001.md` |
| `clinical-safety/DCB0129-001.md` | `.genesis/clinical-safety/DCB0129-001.md` |
| `clinical-safety/DCB0129-001.xlsx` | `.genesis/clinical-safety/DCB0129-001.xlsx` |
| `ig/IG-001.md` | `.genesis/ig/IG-001.md` |
| `security/SEC-001.md` | `.genesis/security/SEC-001.md` |
| `prototype/index.html` | `.genesis/prototype/index.html` |
| `session-close/SESSION-CLOSE-P06.md` | `.genesis/session-close/SESSION-CLOSE-P06.md` |
| `project/PROJECT.md` | `.genesis/project/PROJECT.md` |

**Commit message format:**
```
feat(artefacts): publish requirements/REQ-001.md v1

Triggered-By: user@emisgroup.com
Approved-By: user@emisgroup.com
Project-ID: {uuid}
Artefact-ID: {uuid}
Genesis-AI-Version: 1.0.0.0
```

**Best-effort:** Push failures are logged to `push_failure_log` table and never block the approval. The push-status badge surfaces failures in Project Settings.

**Text vs binary:** Markdown and plain text are read via `GetContentAsync`. Excel and Word files are read via `GetBinaryContentAsync`. The push service detects automatically from content type.

**SHA resolution:** Before every push, the service checks if the file already exists in GitHub via `GetFileShaAsync`. If it does, the PUT includes the current SHA (required by GitHub Contents API for updates). If not, it creates a new file. This is how version 1, 2, 3 etc. of the same artefact are tracked — each approval overwrites the previous version in GitHub, but all versions are preserved in Genesis AI S3/DB.

**Concurrent push safety (ADR-010):** If two users approve artefacts simultaneously, the GitHub Contents API SHA requirement prevents corruption. The second push retries with the updated SHA on 422. No application-level locking needed.

---

## Feature 4 — SESSION-CLOSE

**Where:** Each InProgress pipeline stage card → "Close Session" button

**What it does:** Generates a structured summary document of the current stage session — what was decided, what artefacts were produced, what's still open, what the next stage needs.

**How to use:**

1. Run a pipeline stage session (P01–P08)
2. When ready to capture a summary, click "Close Session" on the stage card
3. The system reads the last 20 messages from the active conversation
4. Generates a `SESSION-CLOSE-P0{n}.md` document via Bedrock
5. Document is saved as an artefact and pushed to `.genesis/session-close/` in the feature repo

**Iterative use:**
- Clicking "Close Session" again creates a new version (v2, v3, etc.)
- The stage conversation is NOT ended — continue working after closing
- To get a better summary: ask the agent to summarise key decisions in chat, then click "Close Session" again
- The last version before stage completion becomes the definitive record in GitHub

**Stage coverage:** P01–P08 only. Normalisation and Planning do not have session-close (no button shown).

**Commit message format:**
```
feat(artefacts): publish session-close/SESSION-CLOSE-P06.md v1

Triggered-By: user@emisgroup.com
Approved-By: user@emisgroup.com
Project-ID: {uuid}
Artefact-ID: {uuid}
Genesis-AI-Version: 1.0.0.0
```

---

## Feature 5 — Push Failure Log

**Where:** `GET /api/v1/projects/{id}/push-status` → Project Settings push-status badge

**What it tracks:** Any artefact that failed to push to GitHub after all retries.

**Push failure log schema:**
- `project_id` — which project
- `artefact_id` — which artefact
- `file_path` — which file
- `error_message` — what went wrong (exception message only, never stack trace)
- `failed_at` — when it failed
- `retry_count` — always 0 (future: retry mechanism)
- `resolved_at` — null until manually resolved

**Common causes:**
- `GITHUB_APP_ID` or `GITHUB_APP_PRIVATE_KEY` env vars missing/wrong
- GitHub App installation not found (wrong installation ID in Project Settings)
- Bot has no write access to the repo (check GitHub App permissions)
- File exceeds 12MB GitHub Contents API limit

---

## Container Test Script

### Prerequisites
- Colima running: `colima start`
- `.env` file with `GITHUB_APP_ID`, `GITHUB_APP_PRIVATE_KEY`, `SECRET_ENCRYPTION_KEY`
- A GitHub repo the App has write access to (installation ID `144995615` covers `genesis-ai-requirements-api` and `genesis-ai-requirements-app`)

### Step 1 — Start the stack
```bash
docker compose up --build -d
docker compose logs -f api | head -20  # confirm API started
```

### Step 2 — Create a test project
1. Open the app at `http://localhost:3000`
2. Create a new project (any code/name)
3. Go to Project Detail → Settings tab

### Step 3 — Test GitHub scaffold
1. In Settings → GitHub Configuration:
   - API Repo URL: `https://github.com/emisgroup/genesis-ai-requirements-api`
   - App Repo URL: `https://github.com/emisgroup/genesis-ai-requirements-app`
2. Click "Save GitHub Configuration"
3. Wait 5–10 seconds
4. Check the feature repo on GitHub — `.genesis/` folder should appear with all subdirectories, `CODEOWNERS`, and `PROJECT.md`
5. Verify commit message includes `Provisioned-By: genesis-ai[bot]` and your ERN as `Triggered-By`

### Step 4 — Test P00 configuration
1. In Settings → P00 Configuration:
   - Release Type: EMIS-X
   - Assurance Required: Yes
   - CSO Role Assigned: Yes
2. Click "Save P00 Configuration"
3. Check `.genesis/project/PROJECT.md` in the feature repo — should contain EMIS-X, Yes, CSO Role Assigned: Yes
4. No ERNs or personal names in the file

### Step 5 — Test artefact push
1. Go to P01 Requirements Discovery
2. Start a conversation, generate a REQ file
3. Approve the REQ file
4. Check `.genesis/requirements/` in the feature repo — `REQ-001.md` should appear
5. Verify commit message includes `Approved-By: {your ERN}`

### Step 6 — Test SESSION-CLOSE
1. In the P01 stage card (InProgress), click "Close Session"
2. Check `.genesis/session-close/SESSION-CLOSE-P01.md` in the feature repo
3. Click "Close Session" again — version should increment to v2

### Step 7 — Test push-status badge
1. Deliberately misconfigure: set GitHub API Repo URL to a non-existent repo
2. Approve any artefact
3. Go to Project Settings — push-status badge should show "1 artefact(s) could not be pushed"
4. Fix the URL, approve another artefact — should push successfully

### Step 8 — Verify CODEOWNERS
1. Check `.genesis/CODEOWNERS` in the feature repo
2. Should contain `@emisgroup/clinical-safety-owners`, `@emisgroup/ig-owners`, `@emisgroup/security-owners`
3. No individual names

---

## What Plan 4c Does NOT Do

- Does not replace the Genesis AI S3/DB as the system of record — GitHub is the audit trail only
- Does not push to GitHub for every save — only for approvals (isPublished: true)
- Does not use local Git binary — GitHub Contents API only (no rebase, no local clone)
- Does not block approvals on push failure — push is always best-effort
- Does not integrate with Figma yet — Wave H after production flag flip
- Does not integrate with CS team hazard tracking DB — parked pending API schema
