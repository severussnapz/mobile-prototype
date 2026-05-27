using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Repositories;
using Genesis.AI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Genesis.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Build NpgsqlDataSource with native enum mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.MapEnum<ComplianceDomain>("compliance_domain");
        dataSourceBuilder.MapEnum<ProjectStatus>("project_status");
        dataSourceBuilder.MapEnum<StageType>("stage_type");
        dataSourceBuilder.MapEnum<PipelineStageStatus>("pipeline_stage_status");
        dataSourceBuilder.MapEnum<ConversationStatus>("conversation_status");
        dataSourceBuilder.MapEnum<ParkingLotPriority>("parking_lot_priority");
        dataSourceBuilder.MapEnum<ParkingLotStatus>("parking_lot_status");
        dataSourceBuilder.MapEnum<MessageRole>("message_role");
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<GenesisAiDbContext>(options =>
            options.UseNpgsql(dataSource, options =>
            {
                options.MapEnum<ComplianceDomain>("compliance_domain");
                options.MapEnum<ProjectStatus>("project_status");
                options.MapEnum<StageType>("stage_type");
                options.MapEnum<PipelineStageStatus>("pipeline_stage_status");
                options.MapEnum<ConversationStatus>("conversation_status");
                options.MapEnum<ParkingLotPriority>("parking_lot_priority");
                options.MapEnum<ParkingLotStatus>("parking_lot_status");
                options.MapEnum<MessageRole>("message_role");
            }));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IArtefactRepository, ArtefactRepository>();
        services.AddSingleton<IAiService, BedrockAiService>();
        services.AddSingleton<IPromptService, EmbeddedPromptService>();
        services.AddSingleton<ISkillContentService, SkillContentService>();

        return services;
    }
}
