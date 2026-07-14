# Genesis AI — API Tool Contracts

## Current Tools (plan4-dom-mutation branch)

### search_in_artefact
Finds elements in a prototype artefact by search query.
```json
{
  "file_path": "prototype/index.html",
  "query": "screen-gallery-file"
}
```
Returns: matched nodes with node_id, tag_name, text_snippet, parent, siblings.

### list_elements
Lists all elements matching a CSS selector within a scoped container.
```json
{
  "selector": "button",
  "scope_node_id": "prototype/fragments/screen-01-legacy.html|5DAD509270A5F907"
}
```
Returns: numbered refs [1]-[N] with text_snippet, parent, siblings.
**Note:** Refs accumulate across calls — never reset within a request.

### set_node_attribute
Sets a single attribute on a single element.
```json
{
  "node_id": "prototype/fragments/screen-01-legacy.html|B15A7D2BB4308787",
  "attribute": "aria-label",
  "value": "Hide document queue panel"
}
```

### set_node_text
Sets the text content of a single element.
```json
{
  "node_id": "...",
  "value": "New text content"
}
```

### add_node_class / remove_node_class
Adds or removes a CSS class from a single element.
```json
{
  "node_id": "...",
  "class_name": "btn-primary"
}
```

### insert_adjacent_html
Inserts HTML adjacent to a target element.
```json
{
  "node_id": "...",
  "position": "beforebegin|afterbegin|beforeend|afterend",
  "html": "<button>New button</button>"
}
```

### remove_element
Removes an element from the DOM.
```json
{
  "node_id": "..."
}
```

### apply_bulk_attributes
**DEPRECATED for bulk operations** — use apply_to_scope when implemented.
Currently works for targeted single-attribute bulk sets but has offset bug for different-value bulk operations.
```json
{
  "attribute": "aria-label",
  "snippet_value_pairs": [
    { "text_snippet": "◀ Hide queue", "value": "Hide document queue panel" },
    { "text_snippet": "Save & close", "value": "Save and close document" }
  ]
}
```

---

## Planned Tools

### apply_to_scope (Plan 3c — PENDING)
The correct pattern for all bulk operations. One call, API executes everything.
```json
{
  "scope": "screen-gallery-file",
  "selector": "button",
  "operation": "set_attribute|add_class|remove_class|set_text|remove_attribute|insert_adjacent_html",
  "attribute": "aria-label",
  "strategy": "derive_from_text_content|literal|generate_from_context",
  "value": "optional for literal strategy"
}
```

**Strategies:**
- `literal` — same value to all matched elements
- `derive_from_text_content` — API cleans TextSnippet per element (strip emoji, arrows, duplicates)
- `generate_from_context` — one focused LLM call, returns [{text_snippet, value}], API matches and applies

---

## node_id Format

```
{fragment_path}|{stable_hash}
// Example:
prototype/fragments/screen-01-legacy.html|B15A7D2BB4308787

// Fallback (no stable id):
prototype/fragments/screen-01-legacy.html|css:#viewer>div>button:nth-child(3)
// Note: css: prefix tells model not to copy this as a hex hash
```

## S3 Structure

```
s3://genesis-ai-artefacts/
  projects/{project_id}/
    artefacts/
      prototype/
        index.html/
          v1, v2, v3...     ← assembled prototype
        fragments/
          screen-01-legacy.html/
            v1, v2, v3...   ← individual screen fragments
      requirements/
        REQ-*.md/
          v1, v2, v3...
      architecture/
        ARCH-*.md/
          v1, v2...
```

## Seed Project IDs (local dev)

```
Project: d0cf7a10-0000-4d0c-8a00-000000000001
Conversation: d0cf7a10-0000-4d0c-8a00-0000000000b2
```
