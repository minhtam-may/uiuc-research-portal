using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using UiucResearchPortal.Domain.Entities;

namespace UiucResearchPortal.Infrastructure.Data.Configurations;

public class PaperConfiguration : IEntityTypeConfiguration<Paper>
{
    /// <summary>
    /// text-embedding-004 (Gemini) outputs 768-dim vectors; adjust if the embedding provider changes.
    /// </summary>
    public const int EmbeddingDimensions = 768;

    public void Configure(EntityTypeBuilder<Paper> builder)
    {
        builder.ToTable("papers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OpenAlexId).IsRequired().HasMaxLength(256);
        builder.HasIndex(p => p.OpenAlexId).IsUnique();

        builder.Property(p => p.Title).IsRequired().HasMaxLength(2048);
        builder.Property(p => p.Doi).HasMaxLength(512);
        builder.Property(p => p.AbstractText).HasColumnType("text");

        builder.Property(p => p.Topics).HasColumnType("text[]");
        builder.Property(p => p.AuthorDisplayNames).HasColumnType("text[]");

        builder.Property(p => p.Embedding)
            .HasColumnType($"vector({EmbeddingDimensions})")
            .HasConversion(
                v => v == null ? null : new Vector(v),
                v => v == null ? null : v.ToArray(),
                new ValueComparer<float[]?>(
                    (a, b) => EmbeddingsEqual(a, b),
                    v => EmbeddingHashCode(v),
                    v => v == null ? null : v.ToArray()));

        builder.HasIndex(p => p.PublicationDate);
    }

    private static bool EmbeddingsEqual(float[]? a, float[]? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.AsSpan().SequenceEqual(b);
    }

    private static int EmbeddingHashCode(float[]? v)
    {
        if (v is null)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach (var x in v)
        {
            hash.Add(x);
        }

        return hash.ToHashCode();
    }
}
