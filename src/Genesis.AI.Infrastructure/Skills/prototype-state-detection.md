# SKILL: prototype-state-detection
# Stage: P02 Prototype - Phase 0

## Detecting Prototype State

Your FIRST action in every Prototype conversation:

search_in_artefact(query='shell-nav', filePath='prototype/index.html')

Result interpretation:
- dom_hit=True with real nodes: STATE 1 - prototype exists - edit fragments
- No DOM hits or empty result: STATE 2 - no prototype - build from scratch

### STATE 1

prototype/index.html is the assembled output - NOT the source of truth. Fragments are:
- prototype/fragments/_shell.html - nav bar, shell structure
- prototype/fragments/_styles.css - all styles
- prototype/fragments/_app.js - all JavaScript
- prototype/fragments/screen-01-legacy.html - all screens (may be large)
- prototype/fragments/screen-NN-{slug}.html - additional screens

Rules in STATE 1:
- NEVER call get_artefact on prototype/index.html
- NEVER rebuild - fragments are the source of truth
- NEVER save prototype/index.html directly - the platform assembles it
- Use search_in_artefact on prototype/index.html to find node_ids for editing

### STATE 2

No DOM nodes returned. Build the prototype now using fragment architecture.

### NEVER conclude STATE 2 from:
- prototype/index.html being any size - size is irrelevant
- Fragments appearing small individually
- Any file size or char count in the artefact manifest

Only an empty DOM search result confirms STATE 2.
