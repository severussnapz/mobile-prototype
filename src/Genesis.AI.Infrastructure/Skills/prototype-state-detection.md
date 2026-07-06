# SKILL: prototype-state-detection
# Stage: P02 Prototype - Phase 0

## Detecting Prototype State

Your FIRST action in every Prototype conversation:

search_in_artefact(query='shell-nav', filePath='prototype/fragments/_shell.html')

Result interpretation:
- Returns matches: STATE 1 - prototype exists - edit fragments
- No matches or file not found: STATE 2 - no prototype - build from scratch

### STATE 1

Fragments are the source of truth. The assembled prototype/index.html is OUTPUT ONLY — never search or edit it.

Fragments:
- prototype/fragments/_shell.html - nav bar, shell structure
- prototype/fragments/_styles.css - all styles
- prototype/fragments/_app.js - all JavaScript
- prototype/fragments/screen-01-legacy.html - migrated legacy screens
- prototype/fragments/screen-NN-{slug}.html - agent-built screens

Rules in STATE 1:
- NEVER call get_artefact on prototype/index.html
- NEVER search prototype/index.html — search the actual fragment files directly
- NEVER rebuild — fragments are the source of truth
- NEVER save prototype/index.html directly — the platform assembles it automatically
- Always search the fragment file (e.g. prototype/fragments/screen-01-legacy.html) to find real selectors

### STATE 2

No fragments found. Build the prototype now using fragment architecture.

### NEVER conclude STATE 2 from:
- prototype/index.html being any size — size is irrelevant
- Fragments appearing small individually
- Any file size or char count in the artefact manifest

Only absence of fragment files confirms STATE 2.
