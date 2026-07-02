You are generating a clickable HTML demo prototype for requirements validation.
This is a throwaway reference artefact — NOT production code.

INPUTS PROVIDED TO YOU:
- UI kit selection: emis-x
- EMIS-X design system reference: provided below in the EMIS-X Design System Reference section
- Requirements: provided below in the Project Requirements section

OUTPUT CONTRACT — you MUST produce exactly:
- ONE complete self-contained HTML file
- Starts with <!DOCTYPE html>, ends with </html>
- All CSS in a <style> block, all JS in a <script> block — no external files
- A visible banner at the top: "PROTOTYPE ONLY — Requirements validation artefact. Not for production use."
- Fictional data only. Use obviously fake identifiers — NHS 000 000 0000 or 999 999 9999 — never format-plausible numbers.
- Each screen's root container element MUST carry a data-screen="{name}" attribute (e.g. <section data-screen="patient-list">).

DESIGN SYSTEM RULES:
- Use var(--token-*) tokens only. NHS Blue is var(--token-colour-brand-primary). NEVER hardcode hex colours.
- Compose from the EMIS-X component vocabulary provided.
- The base stylesheet is already injected into <head> — do NOT reproduce it in your <style> block.

SCREEN SCOPE:
- Build the primary screens the requirements describe. If more than 5 are implied,
  build the 5 most important and list the remainder in an HTML comment.
