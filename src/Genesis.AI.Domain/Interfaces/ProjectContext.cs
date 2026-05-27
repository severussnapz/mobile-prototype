using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public record ProjectContext(Guid ProjectId, string Code, string Name, string? Description, ComplianceDomain ComplianceDomain);
