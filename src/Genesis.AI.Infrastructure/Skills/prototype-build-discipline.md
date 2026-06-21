# SKILL: prototype-build-discipline
# Stage: P02 Prototype - Phases 1-3 (STATE 2 initial build)

## Building a New Prototype - Fragment Architecture

You are in STATE 2 - no prototype exists. Build using fragment architecture.
Never save prototype/index.html directly. The platform assembles it automatically.

### Fragment generation order

One save_artefact call per fragment, in this order:

1. prototype/fragments/_shell.html - nav, layout shell, screen container div
2. prototype/fragments/_styles.css - all CSS, design tokens, component styles
3. prototype/fragments/_app.js - navigation, show/hide, form logic, data constants
4. prototype/fragments/screen-01-{slug}.html - first screen
5. prototype/fragments/screen-02-{slug}.html - second screen
(one fragment per screen, NN = two-digit display order)

### Fragment content rules

_shell.html:
- Nav bar, sidebar, header - everything that appears on every screen
- Screen container: <div id="screen-container"></div>
- Do NOT embed screen content here
- Include prototype banner: WARNING PROTOTYPE ONLY - Requirements validation artefact.

_styles.css - all CSS including:
:root {
  --primary: #2563eb;
  --primary-hover: #1d4ed8;
  --danger: #dc2626;
  --warning: #f59e0b;
  --success: #16a34a;
  --surface: #ffffff;
  --background: #f8fafc;
  --text: #1e293b;
  --text-muted: #64748b;
  --border: #e2e8f0;
  --radius: 8px;
  --shadow: 0 1px 3px rgba(0,0,0,0.1);
}

_app.js - all JavaScript:
- showScreen() function for navigation
- No external dependencies - vanilla JS only

screen-NN-{slug}.html:
- Contains only the screen div: <div class="screen" id="screen-{slug}" data-screen="{slug}">...</div>
- No html/head/body tags - fragment only
- Realistic but obviously fake fictional data

### Fragment save budget

Maximum 15 fragment saves per conversation request. If you need more, call advance_phase or ask the user to continue in a new message.

### Assembly

After every fragment save the platform automatically assembles prototype/index.html. You do not need to trigger or verify assembly.
