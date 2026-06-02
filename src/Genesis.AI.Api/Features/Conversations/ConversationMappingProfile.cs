using AutoMapper;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Api.Features.Conversations;

public class ConversationMappingProfile : Profile
{
    public ConversationMappingProfile()
    {
        CreateMap<Conversation, ConversationResource>()
            .ForMember(
                dest => dest.Status,
                opts => opts.MapFrom(src => src.Status.ToString().ToLowerInvariant()))
            .ForMember(
                dest => dest.Messages,
                opts => opts.MapFrom(src => src.Messages.Count > 0 ? src.Messages.OrderBy(message => message.CreatedAt).ToList() : null))
            .ForMember(
                dest => dest.TokenUsage,
                opts => opts.MapFrom(src => src.TokenUsageRecords.Count > 0
                    ? new TokenUsageSummaryResource
                    {
                        TotalInputTokens = src.TokenUsageRecords.Sum(record => record.InputTokens),
                        TotalOutputTokens = src.TokenUsageRecords.Sum(record => record.OutputTokens),
                        TotalCacheReadTokens = src.TokenUsageRecords.Sum(record => record.CacheReadInputTokens),
                        TotalCacheWriteTokens = src.TokenUsageRecords.Sum(record => record.CacheWriteInputTokens),
                        TurnCount = src.TokenUsageRecords.Count
                    }
                    : null));

        CreateMap<Message, MessageResource>()
            .ForMember(
                dest => dest.Role,
                opts => opts.MapFrom(src => src.Role.ToString().ToLowerInvariant()));
    }
}
