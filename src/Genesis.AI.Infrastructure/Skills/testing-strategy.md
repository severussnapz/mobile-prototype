# SKILL: testing-strategy
# Phase: P04 Design — Phase 9

## Testing Strategy

**Purpose:** Design the test suite for this requirement. This phase is NEVER auto-skipped — testing decisions are requirement-specific.

### Required Test Types

**Unit tests** (xUnit v3 + Moq):
- Command handler: happy path, validation failure, not-found, conflict
- Query handler: returns correct data, handles empty result
- Domain logic: state machine transitions, business rules

**Integration tests** (WebApplicationFactory + Testcontainers):
- End-to-end API: POST → 201, GET → 200, invalid input → 422
- Auth: missing token → 401, wrong scope → 403
- Persistence: save → retrieve cycle

**Test naming convention** (TEST-007): `Method_Scenario_Expected`

### Unit Test Pattern

```csharp
[Fact]
public async Task Handle_{Scenario}_{Expected}()
{
    // Arrange
    var {dependency} = new Mock<I{DependencyName}>();
    {dependency}.Setup(d => d.{Method}(It.IsAny<{Type}>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync({value});

    var handler = new {HandlerName}({dependency}.Object);

    // Act
    var result = await handler.Handle(new {Command}({params}), CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    {dependency}.Verify(d => d.{Method}(...), Times.Once);
}
```

### Test Matrix Template

```markdown
### Testing Strategy

| Test | Type | Scenario | Expected |
|------|------|---------|---------|
| Handle_ValidCommand_CreatesEntity | Unit | Valid input | Success result, entity created |
| Handle_DuplicateKey_ReturnsConflict | Unit | Duplicate | Conflict result |
| POST_{Resource}_ValidInput_Returns201 | Integration | Valid body + auth | 201 Created |
| POST_{Resource}_MissingAuth_Returns401 | Integration | No token | 401 Unauthorized |
| POST_{Resource}_WrongScope_Returns403 | Integration | Wrong scope | 403 Forbidden |
| POST_{Resource}_InvalidBody_Returns422 | Integration | Invalid input | 422 + errors[] |
```
