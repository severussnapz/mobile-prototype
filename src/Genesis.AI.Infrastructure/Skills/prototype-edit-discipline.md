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

## Tool Selection for Prototype Edits

You are in STATE 1 - fragments exist. Pick ONE tool, call it ONCE, trust the result.

### Decision tree

What are you changing?
- One specific HTML element: search_in_artefact to get node_id, then set_node_attribute or set_node_text
- Same property across multiple HTML elements: apply_to_scope - one call, done
- Swap one CSS class for another across multiple elements: apply_to_scope with operation=swap_class, value=old-class:new-class - one call, atomic
- CSS rules or variables: save_artefact on prototype/fragments/_styles.css
- JavaScript functions or logic: save_artefact on prototype/fragments/_app.js
- Shell structure or nav: save_artefact on prototype/fragments/_shell.html
- New screen: save_artefact with path prototype/fragments/screen-NN-{slug}.html
- Existing screen rewrite: save_artefact on that screen fragment only

### Critical rules

DOM tools (set_node_attribute, apply_to_scope) operate on HTML elements only. They cannot rewrite CSS rule text or JavaScript. For CSS or JS changes use save_artefact on the relevant fragment.

One intent = one tool call. If the tool returns success — stop. Do not try an alternative approach. Trust the API result.

After receiving node_ids from search_in_artefact:
1. node_ids are pre-verified — no further search needed
2. Your ONLY next action is the mutation tool
3. Call the mutation tool once per node_id — immediately
4. Do NOT call search_in_artefact, list_artefacts, or get_artefact after receiving node_ids

VIOLATION: Calling any search or list tool after receiving node_ids causes task failure.

### Locating elements before editing

Never assume which fragment owns an element. Always use search_in_artefact on the actual fragment first. Never search prototype/index.html — search the fragment directly (e.g. prototype/fragments/screen-01-legacy.html).

For apply_to_scope, the scope must be the fragment filename without extension.

### apply_to_scope — bulk HTML element changes

Use apply_to_scope when the same change applies to multiple HTML elements.

**MANDATORY PRE-CHECK — apply_to_scope is BLOCKED unless ONE of these is true:**
1. The immediately preceding tool call was search_in_artefact on the same fragment AND returned at least one match, OR
2. The user's message contained raw HTML with the target selector visible

If neither condition is met: call search_in_artefact on the fragment first.
Do NOT proceed to apply_to_scope on a guessed selector.
A silent no-op (zero elements matched) is a task failure, not a success.

**When apply_to_scope returns "NOTHING WAS WRITTEN":**
- STOP — do not retry with another guess
- The API has already identified the confirmed selector in its response
- Use ONLY the selector named in the API response
- Call apply_to_scope once more with that exact selector
- If the API response does not name a confirmed selector, stop and ask the user

Correct:
1. search_in_artefact(file_path="prototype/fragments/screen-01-legacy.html", query="smart view items")
   → confirms elements exist, returns match with real class
2. apply_to_scope({ scope: "screen-01-legacy", selector: "<class from search result>", operation: "set_attribute", attribute: "title", strategy: "derive_from_text_content" })

Wrong: apply_to_scope with a guessed selector before search_in_artefact confirms it exists.

### Class swap pattern

Use swap_class to atomically replace one class with another — ONE call, done.
Verify the source class exists via search_in_artefact before calling swap_class.

apply_to_scope({
  scope: "<fragment-name-without-extension>",
  selector: "<confirmed-class-from-search>",
  operation: "swap_class",
  value: "old-class:new-class",
  strategy: "literal"
})

The API removes old-class and adds new-class atomically on every matched element.
Never use remove_class + add_class as two separate calls for a class swap.

### Assembly

The platform automatically reassembles prototype/index.html after every fragment save or DOM mutation. Do NOT save prototype/index.html directly.
