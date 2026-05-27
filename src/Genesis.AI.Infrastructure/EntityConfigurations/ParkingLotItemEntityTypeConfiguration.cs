using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ParkingLotItemEntityTypeConfiguration : IEntityTypeConfiguration<ParkingLotItem>
{
    public void Configure(EntityTypeBuilder<ParkingLotItem> builder)
    {
        builder.ToTable("parking_lot_item");

        builder.HasKey(parkingLotItem => parkingLotItem.Id);

        builder.Property(parkingLotItem => parkingLotItem.Id)
            .HasColumnName("parking_lot_item_id")
            .ValueGeneratedNever();

        builder.Property(parkingLotItem => parkingLotItem.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(parkingLotItem => parkingLotItem.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(parkingLotItem => parkingLotItem.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(parkingLotItem => parkingLotItem.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(parkingLotItem => parkingLotItem.SourcePhase)
            .HasColumnName("source_phase")
            .IsRequired();

        builder.Property(parkingLotItem => parkingLotItem.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(parkingLotItem => parkingLotItem.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
