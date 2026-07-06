using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;
using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
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
    public DbSet<MessageFeedback> MessageFeedback { get; set; }
    public DbSet<ParkingLotItem> ParkingLotItems { get; set; }
    public DbSet<TokenUsageRecord> TokenUsageRecords { get; set; }
    public DbSet<Artefact> Artefacts { get; set; }
    public DbSet<ProjectNote> ProjectNotes { get; set; }
    public DbSet<ProjectDecision> ProjectDecisions { get; set; }
    public DbSet<UiDelta> UiDeltas { get; set; }
    public DbSet<PrototypeLock> PrototypeLocks { get; set; }
    public DbSet<KnowledgeDocument> KnowledgeDocument { get; set; }
    public DbSet<RequirementChange> RequirementChanges => Set<RequirementChange>();
    public DbSet<HelpConversation> HelpConversation { get; set; }
    public DbSet<HelpMessage> HelpMessage { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        RegisterPostgresEnums(modelBuilder);
        
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        
        if (isInMemory)
        {
            modelBuilder.Ignore<KnowledgeDocument>();
        }
        else
        {
            modelBuilder.Entity<KnowledgeDocument>()
                .Property(k => k.Embedding)
                .HasColumnType("vector(1024)");
        }
        
        ApplyEntityConfigurations(modelBuilder, isInMemory);
        base.OnModelCreating(modelBuilder);
    }

    private void RegisterPostgresEnums(ModelBuilder modelBuilder)
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
            modelBuilder.HasPostgresEnum<RequirementImpact>("requirement_impact");
            modelBuilder.HasPostgresEnum<KnowledgeNamespace>("knowledge_namespace");
        }
    }

    private static void ApplyEntityConfigurations(ModelBuilder modelBuilder, bool isInMemory)
    {
        modelBuilder.ApplyConfiguration(new ProjectEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PipelineStageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ConversationEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new MessageFeedbackEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ParkingLotItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TokenUsageRecordEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ArtefactEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectNoteEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectDecisionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UiDeltaEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PrototypeLockEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RequirementChangeEntityTypeConfiguration());
        
        // KnowledgeDocument is ignored for InMemory (integration tests) but configured for PostgreSQL
        if (!isInMemory)
        {
            modelBuilder.ApplyConfiguration(new KnowledgeDocumentEntityTypeConfiguration());
        }
    }
}
