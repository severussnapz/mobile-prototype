using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpConversationEntityTypeConfiguration : IEntityTypeConfiguration<HelpConversation>
{
    public void Configure(EntityTypeBuilder<HelpConversation> builder)
    {
        builder.ToTable("help_conversation");
    }
}
