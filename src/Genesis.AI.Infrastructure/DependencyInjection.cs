using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Dpia;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.HazardLog;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.SecurityReviewReport;
using Genesis.AI.Infrastructure.Configuration;
using Genesis.AI.Infrastructure.Repositories;
using Genesis.AI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;

namespace Genesis.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddPersistence(services, configuration);

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IArtefactRepository, ArtefactRepository>();
        services.AddScoped<IProjectNoteRepository, ProjectNoteRepository>();
        services.AddScoped<IProjectDecisionRepository, ProjectDecisionRepository>();
        services.AddSingleton<IAiService, BedrockAiService>();
        services.AddSingleton<IPromptService, EmbeddedPromptService>();
        services.AddSingleton<ISkillContentService, SkillContentService>();
        services.AddSingleton<IHazardRegistryParser, HazardRegistryParser>();
        services.AddSingleton<IHazardLogExcelBuilder, HazardLogExcelBuilder>();
        services.AddSingleton<IDpiaDocxBuilder, Pr1625DpiaDocxBuilder>();
        services.AddSingleton<ISecurityReviewReportBuilder, SecurityReviewReportBuilder>();
        services.AddScoped<INormalisationGateService, NormalisationGateService>();
        services.AddScoped<IPlanningGateService, PlanningGateService>();
        services.AddScoped<IFoundationService, FoundationService>();

        services.Configure<TokenOptimisationOptions>(
            configuration.GetSection(TokenOptimisationOptions.SectionName));

        AddS3(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Build NpgsqlDataSource with native enum mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        MapEnums(dataSourceBuilder);
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<GenesisAiDbContext>(options =>
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
            }));
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
