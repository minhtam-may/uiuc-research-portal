using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UiucResearchPortal.Domain.Entities;

namespace UiucResearchPortal.Infrastructure.Data.Configurations;

public class ResearcherConfiguration : IEntityTypeConfiguration<Researcher>
{
    public void Configure(EntityTypeBuilder<Researcher> builder)
    {
        builder.ToTable("researchers");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(r => r.Email).IsUnique();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(256);
        builder.Property(r => r.PhotoUrl).HasMaxLength(1024);
        builder.Property(r => r.Position).HasMaxLength(512);
        builder.Property(r => r.OpenAlexAuthorId).HasMaxLength(128);

        // Stored as a Postgres text[] column; simplest durable mapping for a list of free-text keywords.
        builder.Property(r => r.Keywords)
            .HasColumnType("text[]");
    }
}
