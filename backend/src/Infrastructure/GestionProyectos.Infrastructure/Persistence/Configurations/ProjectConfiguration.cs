using GestionProyectos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        // Soft delete (ver docs/decisions/arquitectura-decisiones.md §7): las filas
        // marcadas is_deleted quedan invisibles para toda consulta LINQ por defecto,
        // sin tener que repetir el filtro en cada Handler.
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Coincidencia parcial por nombre (enunciado seccion 6.3) via ILIKE '%texto%': un
        // B-tree estandar no lo optimiza, se necesita el indice GIN de pg_trgm (ver
        // docs/decisions/arquitectura-decisiones.md §9). La extension se habilita en
        // AppDbContext.OnModelCreating.
        builder.HasIndex(p => p.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_projects_name_trgm");
    }
}
