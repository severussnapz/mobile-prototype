# Genesis AI Requirements App — Copilot Instructions

## Project Overview

Frontend single-page application for the Genesis AI Requirements Platform.
Provides the UI for project management, AI-driven conversations, pipeline
visualisation, and artefact editing.

**Tech Stack:** React 18.3+, TypeScript 5.8+, Webpack 5, React Router v7,
single-spa, pnpm, SCSS Modules, EMIS Design System.

---

## Architecture

### Routing

Uses **React Router v7** with route definitions in `src/pages/Routes.tsx`:

```
/                    → ProjectsPage (list all projects)
/projects            → ProjectsPage
/projects/new        → CreateProjectPage (create form)
/projects/:id        → ProjectDetailPage (overview, tabbed stages/parking-lot/artefacts/usage)
/conversations/:id   → ConversationPage (AI chat interface)
*                    → NotFound (404)
```

### Single-spa Integration

- `root.component.tsx` — Lifecycle entry point (DO NOT MODIFY without Mendeleev
  team approval)
- `App.tsx` — BrowserRouter wrapper with basePath from single-spa props
- Standalone mode (`pnpm start:standalone`) renders full page without ACP host

### Provider Chain

```
root.component → IntlProvider → LaunchDarklyProvider → App → Routes → Pages
```

### API Layer

- `src/api/client.ts` — Axios base instance (baseURL: `/api/v1`, timeout: 30s,
  JSON headers)
- `src/api/projects.ts` — Projects, stages, artefacts, export, project-level
  parking lot
- `src/api/conversations.ts` — Conversations, messages, SSE streaming,
  conversation-level parking lot

### Custom Hooks

- `src/pages/Conversation/hooks/useConversationStream.ts` — Encapsulates all SSE
  streaming logic (send, retry, abort, progress/artefact/parking-lot/ tool-start
  event handling). Exposes `streamingStatus` for real-time tool activity
  feedback, plus `nearLimit` and `toolLimitHit` for tool-turn-limit telemetry.

---

## Key Conventions

### Package Manager

**pnpm only.** Never use npm or yarn. All commands: `pnpm install`, `pnpm add`,
`pnpm test`, etc.

### Styling

- **SCSS Modules** — All styles in `*.module.scss` files
- **Design Tokens** — Colours MUST use `var(--token-name)` (never hex/rgb
  values)
- **Class naming** — camelCase in SCSS modules (enforced by Stylelint)
- **Import pattern:** `@use '~@emisgroup/design-tokens/build/scss/variables'`

### Internationalisation

All user-facing text MUST use translation keys:

```typescript
const { t } = useTranslation();
<h1>{t('Projects.Title')}</h1>
```

Translation file: `src/locales/en-GB/translation.json`. Keys namespaced by
screen.

### UI Components

Use `@emisgroup/ui-*` design system components for all interactive/visual
elements. Native HTML only for semantic structure (`<div>`, `<section>`, `<p>`,
`<h1-h6>`).

Check TypeScript types in `node_modules/@emisgroup/ui-*/dist/index.d.ts` before
using any component.

**Key components in use:**

- `@emisgroup/ui-accordion` — Collapsible sections (Parking Lot panel)
- `@emisgroup/ui-badge` — Status/priority indicators (variants: primary, danger,
  inactive, disabled)
- `@emisgroup/ui-banner` — Inline error messages
- `@emisgroup/ui-breadcrumbs` — Navigation breadcrumbs (Projects > Project Name
  > Stage). Uses `onClickCapture` with `e.preventDefault()` +
  > `e.stopPropagation()` for SPA routing (react-aria intercepts regular clicks)
- `@emisgroup/ui-button` — Actions (variant="filled" for primary, variant="mono"
  for secondary, variant="danger" for destructive)
- `@emisgroup/ui-card` — Project/stage cards
- `@emisgroup/ui-combobox` — Dropdown selects (priority picker)
- `@emisgroup/ui-dialog` — Modal confirmation dialogs (navigation blocker)
- `@emisgroup/ui-input` — Text inputs
- `@emisgroup/ui-progress-indicator` — ProgressBar + ProgressSpinner
- `@emisgroup/ui-table` — Data tables (parking lot, artefacts, token usage)
- `@emisgroup/ui-tabs` — Tabbed content (ProjectDetail: stages, parking lot,
  artefacts, usage)
- `@emisgroup/ui-tag` — Metadata labels (artefact versions)

### HTTP Client

- **Axios** for all standard REST calls (via `src/api/client.ts`)
- **fetch()** for SSE streaming only (axios lacks browser ReadableStream
  support)

### SSE Streaming (Real-time Events)

`conversationsApi.streamMessage()` handles these SSE event types:

| Event                         | Purpose                 | Frontend handler                                   |
| ----------------------------- | ----------------------- | -------------------------------------------------- |
| `data: {"text"}`              | AI text chunk           | `onChunk` — appends to streaming content           |
| `event: tool_start`           | Tool about to execute   | `onToolStart` — updates streaming status indicator |
| `event: progress`             | Phase/progress update   | `onProgress` — merges into sidebar state           |
| `event: artefact`             | File saved              | `onArtefact` — updates artefact list               |
| `event: parking_lot_item`     | Item added              | `onParkingLotItem` — appends to list               |
| `event: parking_lot_resolved` | Item resolved           | `onParkingLotResolved` — updates item status       |
| `event: usage`                | Token usage per AI turn | `onUsage` — updates token usage display            |
| `event: near_limit`           | Tool-turn limit nearing | `onNearLimit` — sets `nearLimit` (early warning)   |
| `event: tool_limit_hit`       | Tool-turn limit reached | `onToolLimitHit` — sets `toolLimitHit` flag        |
| `data: [DONE]`                | Stream complete         | `onDone` — finalises message                       |

Progress and tool_start events fire in real-time as AI tools execute (not
batched at end), so the UI updates live during long responses. The
`streamingStatus` state shows activity like "Saving prototype/index.html..." or
"Reading requirements/REQ-001.md..." with a pulsing indicator.

---

## Running Locally

```bash
# Prerequisites: backend API must be running
cd genesis-ai-requirements-api && docker compose up -d --build

# Start frontend
cd genesis-ai-requirements-app
pnpm install
pnpm start:standalone
# → http://localhost:8080
```

The webpack dev server proxies `/api` → `http://localhost:5000` (backend).

---

## Development Commands

```bash
pnpm start:standalone   # Dev server (standalone mode)
pnpm test               # Run tests
pnpm lint               # ESLint + Prettier + Stylelint
pnpm fix                # Auto-fix lint issues
pnpm format             # Prettier format all files
pnpm tsc --noEmit       # Type check (must exit 0)
pnpm coverage           # Coverage report
pnpm analyze            # Bundle analysis
```

---

## Verification (Before Claiming Done)

All three MUST exit 0:

1. `pnpm tsc --noEmit`
2. `pnpm lint`
3. `pnpm test`

---

## File Structure

```
src/
├── api/                    # API client layer (axios + fetch for SSE)
├── pages/                  # Screen components (one folder per screen)
│   ├── Routes.tsx          # Route definitions
│   ├── Projects/           # Project list
│   ├── CreateProject/      # New project form
│   ├── ProjectDetail/      # Project view + stages + artefacts
│   │   └── components/    # RequirementSelector (per-requirement conversations)
│   ├── Conversation/       # AI chat + progress sidebar + parking lot
│   │   ├── components/    # ProgressSidebar, ParkingLotPanel
│   │   └── hooks/         # useConversationStream (SSE streaming logic)
│   └── NotFound/           # 404
├── components/             # Shared reusable components
├── locales/en-GB/          # Translation strings
├── assets/                 # Static assets (SVG)
├── App.tsx                 # Router wrapper
├── root.component.tsx      # Single-spa entry (managed file)
├── IntlProvider.tsx        # i18n setup
├── LaunchDarklyProvider.tsx # Feature flags
└── Types.ts                # Global interfaces
```

---

## Important Rules

1. **pnpm only** — Never npm or yarn
2. **Design tokens for colours** — `var(--token-name)`, never hex/rgb
3. **Translation keys for all text** — Never hardcode user-facing strings
4. **SCSS Modules with camelCase** — Enforced by Stylelint
5. **Conventional commits** — `feat:`, `fix:`, `refactor:`, etc.
6. **Type-check after every edit** — `pnpm tsc --noEmit` must pass
7. **Don't modify root.component.tsx** — Managed by platform team
8. **Backend at localhost:5000** — Frontend proxies via webpack dev server
9. **No PHI/PII in localStorage/sessionStorage/URLs/logs**
10. **Keyboard accessibility** — All interactive elements must be keyboard
    navigable
11. **Fix Forward** - Always fix forward dont fake passing tests by supressing
    always find a solution

---

## API Types (Keep in Sync with Backend)

```typescript
// Key resource types defined in src/api/projects.ts and src/api/conversations.ts
ProjectResource { id, code, name, description, complianceDomain, status, pipelineStages[] }
PipelineStageResource { id, stageType, status, iteration, sortOrder }
ArtefactResource { id, stageId, version, filePath, contentType, content }
ConversationResource { id, stageId, requirementId, status, messageCount, messages[] }
MessageResource { id, role, content, tokenCount, givenName, familyName, createdAt, images?, documents? }
ConversationProgressResource { currentPhase, phaseName, totalPhases, questionsAsked, estimatedTotalQuestions, requirementsCaptured?, phaseNames, status }
ParkingLotItemResource { id, conversationId, content, priority, status, sourcePhase }
StageTokenUsage { stageId, stageType, inputTokens, outputTokens, cacheReadInputTokens, cacheWriteInputTokens, turnCount, estimatedCost }
ProjectTokenUsageResponse { stages[], totalInputTokens, totalOutputTokens, totalCacheReadInputTokens, totalCacheWriteInputTokens, totalTurnCount, totalEstimatedCost }
TokenUsageEvent { inputTokens, outputTokens, totalTokens, cacheReadInputTokens, cacheWriteInputTokens, cumulativeInputTokens, cumulativeOutputTokens }
```

**API modules:**

- `projectsApi` — CRUD for projects
- `stagesApi` — Stage completion, artefact CRUD
- `projectExportApi` — ZIP download
- `projectParkingLotApi` — Project-scoped parking lot (aggregates across all
  conversations)
- `projectTokenUsageApi` — Aggregated token usage + cost per stage
- `conversationsApi` — Conversation CRUD + SSE streaming (incl.
  `getByStageRequirements` for per-requirement conversation windows)
- `conversationStateApi` — Phase progress, conversation-level parking lot CRUD

### Prototype Preview

The Prototype stage generates a single-file HTML prototype saved as
`prototype/index.html`. The frontend can preview it by:

1. Fetching the artefact content via
   `stagesApi.getArtefact(projectId, artefactId)`
2. Creating a Blob URL: `new Blob([content], { type: 'text/html' })`
3. Opening in a new tab: `window.open(URL.createObjectURL(blob), '_blank')`

This is sandboxed (no access to parent origin cookies/storage). The "Preview
Prototype" button appears:

- On the **ProjectDetail** stage card when `prototype/index.html` artefact
  exists
- In the **Conversation** header (visible only for prototype stage
  conversations)

### Parking Lot Actions

Parking lot items include `conversationId` so the project-level view can call
conversation-level mutation endpoints:

- `conversationStateApi.resolveParkingLotItem(conversationId, itemId)`
- `conversationStateApi.deferParkingLotItem(conversationId, itemId)`
- `conversationStateApi.deleteParkingLotItem(conversationId, itemId)`

The ProjectDetail parking lot tab shows all items (open + resolved/deferred),
with action buttons only on open items.

## Code generation — Ponytail (lazy senior dev mode)

You are a lazy senior developer. Lazy means efficient, not careless. The best code is the code never written.

Before writing any code, stop at the first rung that holds:

1. Does this need to be built at all? (YAGNI)
2. Does it already exist in this codebase? Reuse the helper, util, or pattern that's already here, don't re-write it.
3. Does the standard library already do this? Use it.
4. Does a native platform feature cover it? Use it.
5. Does an already-installed dependency solve it? Use it.
6. Can this be one line? Make it one line.
7. Only then: write the minimum code that works.

The ladder runs after you understand the problem, not instead of it: read the task and the code it touches, trace the real flow end to end, then climb.

Bug fix = root cause, not symptom: a report names a symptom. Grep every caller of the function you touch and fix the shared function once — one guard there is a smaller diff than one per caller, and patching only the path the ticket names leaves a sibling caller still broken.

Rules:
- No abstractions that weren't explicitly requested.
- No new dependency if it can be avoided.
- No boilerplate nobody asked for.
- Deletion over addition. Boring over clever. Fewest files possible.
- Shortest working diff wins, but only once you understand the problem. The smallest change in the wrong place isn't lazy, it's a second bug.
- Question complex requests: "Do you actually need X, or does Y cover it?"
- Pick the edge-case-correct option when two stdlib approaches are the same size — lazy means less code, not the flimsier algorithm.
- Mark intentional simplifications with a `ponytail:` comment. If the shortcut has a known ceiling (global lock, O(n²) scan, naive heuristic), the comment names the ceiling and the upgrade path.

Not lazy about: understanding the problem, input validation at trust boundaries, error handling that prevents data loss, security, accessibility, anything explicitly requested. Non-trivial logic leaves ONE runnable check behind — the smallest thing that fails if the logic breaks. Trivial one-liners need no test.

## Pipeline Engineering Principles — Agent Discipline

**Plan before execution.** The real work happens before a single token is generated. Interrogate the approach, stress-test assumptions, and surface decisions before handing anything to an implementation agent.

**Strong model for planning, cheaper model for execution.** Use the most capable model to interrogate the approach and make decisions. Use a faster model for implementation once the plan is clear. Never use the implementation model to make architectural decisions.

**Fresh agent review as a quality gate.** After any significant output — a prompt, an endpoint, a component — run a review pass with a fresh agent that has no prior context. It catches what the builder normalised. Reviewer sees only the diff and the rules — never the planning conversation. If the validator sees the reasoning it will agree with it, not check against the rule.

**Sub-agents for context window protection.** Scope each agent to one task. Keep context windows bounded. Cherry-pick clean output to the PR branch.

**Interrogate prompts before building.** Before writing implementation code for any prompt-dependent feature, run the prompt concept past a strong model and ask it to find failure modes first.

**Structured decisions — AskUserQuestion.** When the agent reaches a decision point, surface a structured decision — never guess. Present 2-3 options plus Other, reasoning for each, and a recommendation. One question at a time. Never batch decisions. Other always opens free text. Recommendation always stated in one sentence. Applied across all pipeline stages P01-P10.
