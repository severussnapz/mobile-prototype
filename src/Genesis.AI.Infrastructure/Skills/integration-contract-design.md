# SKILL: integration-contract-design
# Phase: P04 Design — Phase 7

## Integration Contract Design

**Purpose:** Design DTOs and contracts for external API integrations identified in P03 Phase 5.

### Questions

For each external integration identified in P03:
1. "What DTO types are needed to call this integration?"
2. "What mapping is required between internal domain types and the external contract?"
3. "What error handling applies when this integration fails?"
4. "Is the contract versioned? Which version?"

### Integration DTO Pattern

```csharp
/// <summary>
/// DTO for {ExternalServiceName} {OperationName} request.
/// Maps from internal {DomainType} to the external contract.
/// </summary>
public sealed record {ExternalServiceName}{OperationName}Request(
    {Type} {ParameterName},
    {Type} {ParameterName2});

public sealed record {ExternalServiceName}{OperationName}Response(
    {Type} {PropertyName},
    {Type} {PropertyName2});
```

### AutoMapper Profile Pattern

```csharp
public sealed class {FeatureName}MappingProfile : Profile
{
    public {FeatureName}MappingProfile()
    {
        CreateMap<{DomainType}, {ExternalDto}>();
        CreateMap<{ExternalDto}, {DomainType}>();
    }
}
```

### Integration Contract Template

```markdown
### Integration Contracts

| Integration | Operation | Request DTO | Response DTO | Error handling |
|------------|-----------|------------|-------------|----------------|
| {ServiceName} | {Operation} | {RequestDto} | {ResponseDto} | {Circuit breaker / retry / fallback} |
```
