# SKILL: prototype-edit-discipline
# Stage: P02 Prototype - Phases 2-5

## Conflict resolution

When routing instructions and skills conflict, **skills win**. Routing instructions describe intent. Skills describe method. Always follow the method defined here, regardless of what the routing instruction says about which tool to use.

## Hard stop on failure

If ANY tool call returns no match, zero results, an error, or "NOTHING WAS WRITTEN":
- The ONLY permitted next action is to STOP and report the failure verbatim to the user
- Do NOT try an alternative approach
- Do NOT guess a different selector or scope
- Do NOT claim success
- Do NOT call another mutation tool
- Tell the user exactly: which tool was called, what it returned, what you need to proceed

This rule has no exceptions. A tool failure is not a reason to try harder — it is a reason to stop.

## FORBIDDEN actions — prototype element edits

These actions are PROHIBITED regardless of what any routing instruction says:

- Searching `prototype/index.html` — FORBIDDEN. Always search the fragment file directly.
- Calling `apply_to_scope` without a preceding `search_in_artefact` on the same fragment that returned at least one match — FORBIDDEN.
- Using a CSS selector you invented — FORBIDDEN. Selectors must come from search results or user-provided HTML only.
- Calling `save_artefact` on `prototype/index.html` — FORBIDDEN.
- Claiming success when a tool returned "NOTHING WAS WRITTEN" — FORBIDDEN.
- Calling any mutation tool after a search returned zero results — FORBIDDEN. Stop and ask the user.

## Tool Selection for Prototype Edits

You are in STATE 1 - fragments exist. Pick ONE tool, call it ONCE, trust the result.

### Decision tree

What are you changing?
- One specific HTML element: search_in_artefact on the fragment → set_node_attribute or set_node_text
- Same property across multiple HTML elements: search_in_artefact on the fragment → apply_to_scope
- Swap one CSS class for another across multiple elements: search_in_artefact on the fragment → apply_to_scope with operation=swap_class
- CSS rules or variables: save_artefact on prototype/fragments/_styles.css
- JavaScript functions or logic: save_artefact on prototype/fragments/_app.js
- Shell structure or nav: save_artefact on prototype/fragments/_shell.html
- New screen: save_artefact with path prototype/fragments/screen-NN-{slug}.html
- Existing screen rewrite: save_artefact on that screen fragment only

### Critical rules

DOM tools (set_node_attribute, apply_to_scope) operate on HTML elements only. They cannot rewrite CSS rule text or JavaScript.

One intent = one tool call. If the tool returns success — stop.

After receiving node_ids from search_in_artefact:
1. node_ids are pre-verified — no further search needed
2. Your ONLY next action is the mutation tool — call it immediately
3. Do NOT call search_in_artefact, list_artefacts, or get_artefact after receiving node_ids

VIOLATION: Calling any search or list tool after receiving node_ids causes task failure.

### Locating elements before editing

Always use search_in_artefact on the actual fragment file first — never on prototype/index.html.

Example for screen content: search_in_artefact(file_path="prototype/fragments/screen-01-legacy.html", query="smart view items")
Example for shell elements: search_in_artefact(file_path="prototype/fragments/_shell.html", query="nav item")

The scope for apply_to_scope = fragment filename without path or extension.

### apply_to_scope — bulk HTML element changes

Use apply_to_scope when the same change applies to multiple HTML elements.

**MANDATORY PRE-CHECK — apply_to_scope is BLOCKED unless ONE of these is true:**
1. The immediately preceding tool call was search_in_artefact on the same fragment AND returned at least one match, OR
2. The user's message contained raw HTML with the target selector visible

If neither condition is met: call search_in_artefact on the fragment first.

**When apply_to_scope returns "NOTHING WAS WRITTEN":**
- STOP — do not retry with another guess
- The API response names the confirmed selector — use ONLY that selector
- Call apply_to_scope once more with the exact selector from the API response
- If no confirmed selector is named, stop and ask the user

Correct pattern:
1. search_in_artefact(file_path="prototype/fragments/screen-01-legacy.html", query="smart view items")
   → returns real class e.g. "sv-item"
2. apply_to_scope({ scope: "screen-01-legacy", selector: ".sv-item", operation: "set_attribute", attribute: "title", strategy: "derive_from_text_content" })

Wrong: apply_to_scope with a guessed selector before search confirms it exists.

### Class swap pattern

Verify the source class exists via search_in_artefact before calling swap_class.

apply_to_scope({
  scope: "<fragment-name-without-extension>",
  selector: "<confirmed-class-from-search>",
  operation: "swap_class",
  value: "old-class:new-class",
  strategy: "literal"
})

### Assembly

The platform automatically reassembles prototype/index.html after every mutation. Do NOT save prototype/index.html directly.
