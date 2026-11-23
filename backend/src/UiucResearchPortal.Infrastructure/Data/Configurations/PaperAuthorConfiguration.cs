using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UiucResearchPortal.Domain.Entities;

namespace UiucResearchPortal.Infrastructure.Data.Configurations;

public class PaperAuthorConfiguration : IEntityTypeConfiguration<PaperAuthor>
{
    public void Configure(EntityTypeBuilder<PaperAuthor> builder)
    {
        builder.ToTable("paper_authors");

        builder.HasKey(pa => new { pa.PaperId, pa.ResearcherId });

        builder.HasOne(pa => pa.Paper)
            .WithMany(p => p.PaperAuthors)
            .HasForeignKey(pa => pa.PaperId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Researcher)
            .WithMany(r => r.PaperAuthors)
            .HasForeignKey(pa => pa.ResearcherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
