# SKILL: data-validation-rules
# Phase: P04 Design — Phase 5

## Data Validation Rules

**Purpose:** Define input validation and business rules using FluentValidation.

### For EACH Requirement

1. "What inputs need validation?" → NHS number, date of birth, email, amounts
2. "What are the validation rules?" → Format (regex), length, range, required
3. "What are the user-friendly error messages?"
4. "Any business rules?" → Age >18, prescription requires allergy check, etc.

### FluentValidation Pattern (Mandatory — no Data Annotations)

```csharp
public class {RequestType}Validator : AbstractValidator<{RequestType}>
{
    public {RequestType}Validator()
    {
        RuleFor(request => request.{FieldName})
            .NotEmpty()
            .WithMessage("{Field} is required")
            .Matches(@"{regex}")
            .WithMessage("{User-friendly error message}")
            .Must({BusinessRule})
            .WithMessage("{Business rule error message}");
    }
}
```

**Rules:**
- Use `AbstractValidator<T>` from FluentValidation (ENG-008)
- No `[Required]` / `[MaxLength]` Data Annotations — FluentValidation only
- Descriptive parameter names in `.Must()` lambdas (ENG-011)
- Error messages must be user-friendly (not technical)

### Validation Specification Template

```markdown
### Data Validation Rules

| Field | Rule | Error Message |
|-------|------|--------------|
| {FieldName} | Required | "{Field} is required" |
| {FieldName} | Matches {pattern} | "{Field} format is invalid" |
| {FieldName} | Business rule: {description} | "{Business error message}" |
```

### Validation Summary

```
"Validation rules for REQ-{NNN}:
- Fields: {list with rules}
- Business rules: {list}
- Validators: {list of validator class names}

Correct?"
```
