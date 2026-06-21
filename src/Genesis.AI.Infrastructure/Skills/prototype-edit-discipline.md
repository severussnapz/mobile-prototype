# SKILL: prototype-edit-discipline
# Stage: P02 Prototype - Phases 2-5

## Tool Selection for Prototype Edits

You are in STATE 1 - fragments exist. Pick ONE tool, call it ONCE, trust the result.

### Decision tree

What are you changing?
- One specific HTML element: search_in_artefact to get node_id, then set_node_attribute or set_node_text
- Same property across multiple HTML elements: apply_to_scope - one call, done
- CSS rules or variables: save_artefact on prototype/fragments/_styles.css
- JavaScript functions or logic: save_artefact on prototype/fragments/_app.js
- Shell structure or nav: save_artefact on prototype/fragments/_shell.html
- New screen: save_artefact with path prototype/fragments/screen-NN-{slug}.html
- Existing screen rewrite: save_artefact on that screen fragment only

### Critical rules

DOM tools (set_node_attribute, apply_to_scope) operate on HTML elements only. They cannot rewrite CSS rule text or JavaScript. For CSS or JS changes use save_artefact on the relevant fragment.

One intent = one tool call. If the tool returns success - stop. Do not try an alternative approach. Trust the API result.

After receiving node_ids from search_in_artefact:
1. node_ids are pre-verified - no further search needed
2. Your ONLY next action is the mutation tool
3. Call the mutation tool once per node_id - immediately
4. Do NOT call search_in_artefact, list_artefacts, or get_artefact after receiving node_ids

VIOLATION: Calling any search or list tool after receiving node_ids causes task failure.

### apply_to_scope - bulk HTML element changes

Use apply_to_scope when the same change applies to multiple HTML elements.

Correct:
apply_to_scope({
  scope: "screen-filing-queue",
  selector: "button.btn-primary",
  operation: "remove_class",
  value: "btn-primary"
})

Wrong: search for each button, then set_node_attribute x N - causes offset errors.

### Assembly

The platform automatically reassembles prototype/index.html after every fragment save or DOM mutation. Do NOT save prototype/index.html directly.
