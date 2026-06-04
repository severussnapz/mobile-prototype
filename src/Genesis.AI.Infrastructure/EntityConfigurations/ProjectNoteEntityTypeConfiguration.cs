using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ProjectNoteEntityTypeConfiguration : IEntityTypeConfiguration<ProjectNote>
{
    public void Configure(EntityTypeBuilder<ProjectNote> builder)
    {
        builder.ToTable("project_note");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Id)
            .HasColumnName("project_note_id")
            .ValueGeneratedNever();

        builder.Property(note => note.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(note => note.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(note => note.AuthorErn)
            .HasColumnName("author_ern")
            .HasMaxLength(200);

        builder.Property(note => note.AuthorGivenName)
            .HasColumnName("author_given_name")
            .HasMaxLength(100);

        builder.Property(note => note.AuthorFamilyName)
            .HasColumnName("author_family_name")
            .HasMaxLength(100);

        builder.Property(note => note.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(note => note.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(note => note.ProjectId)
            .HasDatabaseName("idx_project_note_project_id");
    }
}
