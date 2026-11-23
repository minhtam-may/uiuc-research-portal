using Microsoft.EntityFrameworkCore;
using UiucResearchPortal.Domain.Entities;

namespace UiucResearchPortal.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Researcher> Researchers => Set<Researcher>();

    public DbSet<Paper> Papers => Set<Paper>();

    public DbSet<PaperAuthor> PaperAuthors => Set<PaperAuthor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
