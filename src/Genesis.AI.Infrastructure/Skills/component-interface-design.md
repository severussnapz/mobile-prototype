# SKILL: component-interface-design
# Phase: P04 Design — Phase 3

## Component Interface Design

**Purpose:** Design C# interfaces, dependency injection contracts, and EMIS-X frontend component specifications.

### Backend: C# Interface Design

For each service component, design the interface:

```csharp
/// <summary>
/// {Description of what this interface does}
/// </summary>
public interface I{ServiceName}
{
    Task<{ResultType}> {OperationName}Async(
        {ParameterType} {parameterName},
        CancellationToken cancellationToken);
}
```

**Rules:**
- One interface per domain concern (SRP)
- All async methods take `CancellationToken cancellationToken` as the last parameter
- No single-letter parameter names (ENG-007 / ENG-011)
- Return `Task<T>` not `Task<IActionResult>`
- Repository interfaces: `GetByIdAsync`, `ListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`

### Frontend: EMIS-X Component Mandates

Before designing frontend components, confirm non-negotiable standards:

**No native HTML interactive elements** — always use @emisgroup/ui-* equivalents:

| Prohibited | Required |
|-----------|---------|
| `<button>` | `<Button>` from `@emisgroup/ui-button` |
| `<input>` | `<Input>` from `@emisgroup/ui-input` |
| `<select>` | `<Combobox>` from `@emisgroup/ui-combobox` |
| `<table>` | `<Table>` from `@emisgroup/ui-table` |
| `<dialog>` | `<Dialog>` from `@emisgroup/ui-dialog` |

**Design tokens for all colours** — `var(--token-name)` not hex/rgb.

**Translation keys for all text** — `t('Namespace.Key')` not hardcoded strings.

**displayName required** — every component must set `ComponentName.displayName = 'ComponentName'`.

### Interface Template

```markdown
### Component Interfaces

**Backend:**
- `I{ServiceName}`: {methods}
- `I{RepositoryName}`: {methods}

**Frontend component specs:**
- `{ComponentName}` ({@emisgroup/ui-*} based): {props}, {state}, {events}
```
