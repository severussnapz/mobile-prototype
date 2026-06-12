using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.EntityConfigurations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure;

public sealed class GenesisAiDbContext(
    DbContextOptions<GenesisAiDbContext> options,
    IMediator mediator) : DatabaseContext(options, mediator)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<PipelineStage> PipelineStages { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ParkingLotItem> ParkingLotItems { get; set; }
    public DbSet<TokenUsageRecord> TokenUsageRecords { get; set; }
    public DbSet<Artefact> Artefacts { get; set; }
    public DbSet<ProjectNote> ProjectNotes { get; set; }
    public DbSet<ProjectDecision> ProjectDecisions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Native PostgreSQL enum types — skip for in-memory provider (integration tests)
        if (!Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            modelBuilder.HasPostgresEnum<ComplianceDomain>("compliance_domain");
            modelBuilder.HasPostgresEnum<ProjectStatus>("project_status");
            modelBuilder.HasPostgresEnum<StageType>("stage_type");
            modelBuilder.HasPostgresEnum<PipelineStageStatus>("pipeline_stage_status");
            modelBuilder.HasPostgresEnum<ConversationStatus>("conversation_status");
            modelBuilder.HasPostgresEnum<ParkingLotPriority>("parking_lot_priority");
            modelBuilder.HasPostgresEnum<ParkingLotStatus>("parking_lot_status");
            modelBuilder.HasPostgresEnum<MessageRole>("message_role");
            modelBuilder.HasPostgresEnum<OrchestrationMode>("orchestration_mode");
        }

        modelBuilder.ApplyConfiguration(new ProjectEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PipelineStageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ParkingLotItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TokenUsageRecordEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ArtefactEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectNoteEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectDecisionEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
