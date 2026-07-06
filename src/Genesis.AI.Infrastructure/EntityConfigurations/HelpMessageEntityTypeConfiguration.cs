using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpMessageEntityTypeConfiguration : IEntityTypeConfiguration<HelpMessage>
{
    public void Configure(EntityTypeBuilder<HelpMessage> builder)
    {
        builder.ToTable("help_message");
    }
}
