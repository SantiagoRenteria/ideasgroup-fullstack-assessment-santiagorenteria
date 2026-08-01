using GestionProyectos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Requerida por el indice GIN de coincidencia parcial en Project.Name (ver
        // ProjectConfiguration y docs/decisions/arquitectura-decisiones.md §9).
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
