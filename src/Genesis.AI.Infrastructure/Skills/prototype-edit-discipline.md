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
- Calling `insert_adjacent_html` as a standalone tool — FORBIDDEN. It has no standalone handler. Use apply_to_scope with operation=insert_adjacent_html only.
- Calling search_in_artefact when the user's message contains an HTML element with a class attribute — FORBIDDEN. Extract the class name directly and call apply_to_scope immediately with that selector.
- For `insert_adjacent_html`, `strategy` MUST be `literal` and `value` MUST contain the exact HTML to insert — FORBIDDEN to use `generate_from_context` for HTML insertion.

## Tool Selection for Prototype Edits

You are in STATE 1 - fragments exist. Pick ONE tool, call it ONCE, trust the result.

### Decision tree

What are you changing?
- One specific HTML element attribute: search_in_artefact on the fragment → apply_to_scope with operation=set_attribute
- Visible text of one specific HTML element: search_in_artefact on the fragment → apply_to_scope with operation=set_text
- Same property across multiple HTML elements: search_in_artefact on the fragment → apply_to_scope
- Add or remove a CSS class on elements: search_in_artefact on the fragment → apply_to_scope with operation=add_class or operation=remove_class
- Add new HTML element near existing elements: search_in_artefact to confirm selector → apply_to_scope with operation=insert_adjacent_html (attribute=afterend to insert after, afterbegin to insert inside at start, beforeend to insert inside at end)
  ⛔ insert_adjacent_html is an OPERATION inside apply_to_scope — NEVER a standalone tool call. Standalone calls silently fail.
  ⛔ For insert_adjacent_html, strategy MUST be literal. Provide the exact HTML in value. generate_from_context is invalid for HTML insertion.
  ⛔ The inserted markup MAY introduce a new class (e.g. a new badge). If it does, you MUST follow up with save_artefact on prototype/fragments/_styles.css to style that class in the same edit session — otherwise the new element renders unstyled. Only the anchor selector you target must already exist.
- Swap one CSS class for another across multiple elements: search_in_artefact on the fragment → apply_to_scope with operation=swap_class
- CSS rules or variables: save_artefact on prototype/fragments/_styles.css
- JavaScript functions or logic: save_artefact on prototype/fragments/_app.js
- Shell structure or nav: save_artefact on prototype/fragments/_shell.html
- New screen: save_artefact with path prototype/fragments/screen-NN-{slug}.html
- Existing screen rewrite: save_artefact on that screen fragment only

### Critical rules

DOM tools (apply_to_scope) operate on HTML elements only. They cannot rewrite CSS rule text or JavaScript.

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
