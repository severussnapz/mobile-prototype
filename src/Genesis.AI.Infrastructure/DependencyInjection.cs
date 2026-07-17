using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Commands.ApproveRequirementChange;
using Genesis.AI.Domain.Commands.ProposeRequirementChange;
using Genesis.AI.Domain.Commands.RecordDomainReview;
using Genesis.AI.Domain.Commands.RejectRequirementChange;
using Genesis.AI.Domain.Commands.ReopenStageForAmendment;
using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Commands.UndoApproveRequirementChange;
using Genesis.AI.Domain.Dpia;
using Genesis.AI.Domain.HazardLog;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.SecurityReviewReport;
using Genesis.AI.Core.Data;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.EventHandlers;
using Genesis.AI.Infrastructure.Repositories;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Infrastructure.Services.GitHub;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using System.Globalization;

namespace Genesis.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);

        AddRepositories(services);
        AddPipelineServices(services);
        AddDocumentBuilders(services);
        AddGitHubIntegration(services, configuration);
        AddWorkflowServices(services);

        services.Configure<TokenOptimisationOptions>(
            configuration.GetSection(TokenOptimisationOptions.SectionName));

        AddS3(services, configuration);

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageFeedbackRepository, MessageFeedbackRepository>();
        services.AddScoped<IArtefactRepository, ArtefactRepository>();
        services.AddScoped<IContractManifestRepository, ContractManifestRepository>();
        services.AddScoped<IProjectNoteRepository, ProjectNoteRepository>();
        services.AddScoped<IProjectDecisionRepository, ProjectDecisionRepository>();
        services.AddScoped<IUiDeltaRepository, UiDeltaRepository>();
        services.AddScoped<IPrototypeLockRepository, PrototypeLockRepository>();
        services.AddScoped<IRequirementChangeRepository, RequirementChangeRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IHelpConversationRepository, HelpConversationRepository>();
        services.AddScoped<IHelpChatStreamService, HelpChatStreamService>();
        services.AddScoped<IPushFailureLogRepository, PushFailureLogRepository>();
        services.AddHostedService<KnowledgeSeederService>();
    }

    private static void AddPipelineServices(IServiceCollection services)
    {
        services.AddSingleton<IAiService, BedrockAiService>();
        services.AddScoped<IEmbeddingService, BedrockEmbeddingService>();
        services.AddScoped<IKnowledgeService, BedrockKnowledgeService>();
        services.AddScoped<INotificationHandler<ArtefactPublishedDomainEvent>, ArtefactPublishedDomainEventHandler>();
        services.AddSingleton<IPromptService, EmbeddedPromptService>();
        services.AddSingleton<ISkillContentService, SkillContentService>();
        services.AddSingleton<IActiveSkillsService, ActiveSkillsService>();
        services.AddScoped<IRoutingContextService, RoutingContextService>();
    }

    private static void AddDocumentBuilders(IServiceCollection services)
    {
        services.AddSingleton<IHazardRegistryParser, HazardRegistryParser>();
        services.AddSingleton<IHazardLogExcelBuilder, HazardLogExcelBuilder>();
        services.AddSingleton<IDpiaDocxBuilder, Pr1625DpiaDocxBuilder>();
        services.AddSingleton<ISecurityReviewReportBuilder, SecurityReviewReportBuilder>();
    }

    private static void AddGitHubIntegration(IServiceCollection services, IConfiguration configuration)
    {
        var gitHubConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_APP_ID"));

        if (!gitHubConfigured)
        {
            services.AddSingleton<ISecretEncryptionService, NoOpSecretEncryptionService>();
            services.AddSingleton<IGitHubTokenService, NoOpGitHubTokenService>();
            services.AddScoped<IGitHubArtefactPushService, NoOpGitHubArtefactPushService>();
            services.AddSingleton<IAssemblyVersionProvider, AssemblyVersionProvider>();
            services.AddSingleton<ICodeownersGenerator, CodeownersGenerator>();
            services.AddScoped<IProjectMarkdownGenerator, ProjectMarkdownGenerator>();
            services.AddScoped<IGenesisStructureScaffolder, GenesisStructureScaffolder>();
            return;
        }

        services.AddSingleton<ISecretEncryptionService, AesSecretEncryptionService>();
        services.AddSingleton<IAssemblyVersionProvider, AssemblyVersionProvider>();
        services.AddSingleton<ICodeownersGenerator, CodeownersGenerator>();
        services.AddScoped<IProjectMarkdownGenerator, ProjectMarkdownGenerator>();
        services.AddScoped<IGenesisStructureScaffolder, GenesisStructureScaffolder>();
        services.AddHttpClient<GitHubAppTokenService>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("genesis-ai");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(30);
            options.Retry.UseJitter = true;
        });

        services.AddSingleton<IGitHubTokenService, GitHubAppTokenService>();

        services.AddHttpClient<IGitHubContentsService, GitHubContentsService>((_, client) =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("genesis-ai");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(30);
            options.Retry.UseJitter = true;
        });

        services.AddScoped<IGitHubArtefactPushService, GitHubArtefactPushService>();
    }

    private static void AddWorkflowServices(IServiceCollection services)
    {
        services.AddScoped<INormalisationGateService, NormalisationGateService>();
        services.AddScoped<IPlanningGateService, PlanningGateService>();
        services.AddScoped<IFoundationService, FoundationService>();
        services.AddScoped<ISessionCloseContextBuilder, SessionCloseContextBuilder>();
        services.AddScoped<IContractManifestContextBuilder, ContractManifestContextBuilder>();
        services.AddScoped<IPrototypeAssemblyService, PrototypeAssemblyService>();
        services.AddScoped<IPrototypeDomSearchService, PrototypeDomSearchService>();
        services.AddScoped<IPrototypeDemoEditService, BedrockPrototypeDemoEditService>();
        services.AddScoped<IPrototypeDomMutationService, PrototypeDomMutationService>();
        services.AddScoped<StructuralEditDraftService>();
        services.AddScoped<IStructuralEditReorderService, StructuralEditReorderService>();
        services.AddScoped<IStructuralEditMutationService, StructuralEditMutationService>();
        services.AddScoped<IStructuralEditService, StructuralEditService>();
        services.AddScoped<IRequirementImpactClassifier, RequirementImpactClassifier>();
        services.AddScoped<ISessionCloseSkillBuilder, SessionCloseSkillBuilder>();
        services.AddScoped<IRequirementsFeedbackLoopService, RequirementsFeedbackLoopService>();
        services.AddScoped<IChangeFileWriterService, ChangeFileWriterService>();
        services.AddScoped<IContractValidationService, ContractValidationService>();
        services.AddScoped<IPipelineReadinessService, PipelineReadinessService>();
        services.AddScoped<GenerateSessionCloseCommandHandler>();
        services.AddScoped<ProposeRequirementChangeCommandHandler>();
        services.AddScoped<IPrototypeFragmentMigrationService, PrototypeFragmentMigrationService>();
        services.AddScoped<ApproveRequirementChangeCommandHandler>();
        services.AddScoped<UndoApproveRequirementChangeCommandHandler>();
        services.AddScoped<RejectRequirementChangeCommandHandler>();
        services.AddScoped<RecordDomainReviewCommandHandler>();
        services.AddScoped<ReopenStageForAmendmentCommandHandler>();
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        // Build NpgsqlDataSource with native enum mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.UseVector();
        MapEnums(dataSourceBuilder);

        // In AWS, authenticate to RDS with short-lived IAM tokens rather than a
        // static password. The presence of RDS_HOST signals a deployed
        // environment; local development keeps using the password embedded in
        // the DefaultConnection string.
        var rdsHost = configuration["RDS_HOST"];
        if (!string.IsNullOrEmpty(rdsHost))
        {
            var rdsPort = int.Parse(configuration["RDS_PORT"] ?? "5432", CultureInfo.InvariantCulture);
            var rdsUser = configuration["RDS_USER"] ?? "genesis_ai_app";

            dataSourceBuilder.UsePeriodicPasswordProvider(
                (_, _) =>
                {
                    // IAM auth tokens are valid for 15 minutes; the default
                    // credential chain (EKS Pod Identity) and AWS_REGION supply
                    // the signing context.
                    var token = RDSAuthTokenGenerator.GenerateAuthToken(rdsHost, rdsPort, rdsUser);
                    return ValueTask.FromResult(token);
                },
                TimeSpan.FromMinutes(10),  // refresh ahead of the 15-minute expiry
                TimeSpan.FromMinutes(1));   // retry quickly on failure
        }

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<GenesisAiDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<ComplianceDomain>("compliance_domain");
                npgsqlOptions.MapEnum<ProjectStatus>("project_status");
                npgsqlOptions.MapEnum<StageType>("stage_type");
                npgsqlOptions.MapEnum<PipelineStageStatus>("pipeline_stage_status");
                npgsqlOptions.MapEnum<ConversationStatus>("conversation_status");
                npgsqlOptions.MapEnum<ParkingLotPriority>("parking_lot_priority");
                npgsqlOptions.MapEnum<ParkingLotStatus>("parking_lot_status");
                npgsqlOptions.MapEnum<MessageRole>("message_role");
                npgsqlOptions.MapEnum<OrchestrationMode>("orchestration_mode");
                npgsqlOptions.MapEnum<RequirementImpact>("requirement_impact");
                npgsqlOptions.MapEnum<KnowledgeNamespace>("knowledge_namespace");
                npgsqlOptions.UseVector();
            });
        });

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<GenesisAiDbContext>());
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        // In AWS the connection string is assembled from the RDS_* settings and
        // uses IAM authentication (no password). Locally we use the full
        // connection string supplied in configuration.
        var rdsHost = configuration["RDS_HOST"];
        if (!string.IsNullOrEmpty(rdsHost))
        {
            var rdsPort = configuration["RDS_PORT"] ?? "5432";
            var rdsDbName = configuration["RDS_DB_NAME"] ?? "genesis_ai_requirements";
            var rdsUser = configuration["RDS_USER"] ?? "genesis_ai_app";

            // Keep a small warm pool so the hot path (readiness probe, requests)
            // reuses an already-authenticated connection instead of paying the
            // slow IAM cold-connect cost (token + SSL + IAM validation) every
            // time. Keepalive stops NAT/server idle timeouts silently dropping
            // the warm connections.
            return $"Host={rdsHost};Port={rdsPort};Database={rdsDbName};Username={rdsUser};SSL Mode=Require;Trust Server Certificate=true;Minimum Pool Size=2;Keepalive=30";
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private static void MapEnums(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.MapEnum<ComplianceDomain>("compliance_domain");
        dataSourceBuilder.MapEnum<ProjectStatus>("project_status");
        dataSourceBuilder.MapEnum<StageType>("stage_type");
        dataSourceBuilder.MapEnum<PipelineStageStatus>("pipeline_stage_status");
        dataSourceBuilder.MapEnum<ConversationStatus>("conversation_status");
        dataSourceBuilder.MapEnum<ParkingLotPriority>("parking_lot_priority");
        dataSourceBuilder.MapEnum<ParkingLotStatus>("parking_lot_status");
        dataSourceBuilder.MapEnum<MessageRole>("message_role");
        dataSourceBuilder.MapEnum<OrchestrationMode>("orchestration_mode");
        dataSourceBuilder.MapEnum<RequirementImpact>("requirement_impact");
        dataSourceBuilder.MapEnum<KnowledgeNamespace>("knowledge_namespace");
    }

    private static void AddS3(IServiceCollection services, IConfiguration configuration)
    {
        // Local development points at LocalStack via S3:ServiceUrl. In deployed
        // environments the key is absent and the default IAM credential chain is
        // used with the configured region (S3-002).
        var serviceUrl = configuration["S3:ServiceUrl"];
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                new BasicAWSCredentials("test", "test"),
                new AmazonS3Config
                {
                    ServiceURL = serviceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = "eu-west-2"
                }));
        }
        else
        {
            services.AddAWSService<IAmazonS3>(new AWSOptions
            {
                Region = RegionEndpoint.EUWest2
            });
        }

        services.AddSingleton<IArtefactStorageService, S3ArtefactStorageService>();
    }
}
