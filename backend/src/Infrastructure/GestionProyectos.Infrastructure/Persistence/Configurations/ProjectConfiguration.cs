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

        // Dos indices distintos sobre la misma columna: se nombran directamente en HasIndex
        // (no solo via HasDatabaseName encadenado) porque EF Core identifica un indice por
        // su lista de propiedades -- sin el nombre explicito aqui, la segunda llamada a
        // HasIndex(p => p.Name) reconfigura la MISMA entrada de metadata en vez de crear
        // una nueva, y el "unique" terminaba fusionado con el metodo "gin" del primero
        // (Postgres no permite indices unicos con access method gin).

        // Coincidencia parcial por nombre (enunciado seccion 6.3) via ILIKE '%texto%': un
        // B-tree estandar no lo optimiza, se necesita el indice GIN de pg_trgm (ver
        // docs/decisions/arquitectura-decisiones.md §9). La extension se habilita en
        // AppDbContext.OnModelCreating.
        builder.HasIndex(p => p.Name, "ix_projects_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Defensa en profundidad contra la condicion de carrera de dos altas simultaneas
        // con el mismo nombre (la comprobacion real, case-insensitive, vive en
        // CreateProjectCommandHandler/UpdateProjectCommandHandler via ExistsByNameAsync).
        // Case-sensitive porque un indice unico por expresion (lower(name)) no es soportado
        // directamente por el Fluent API de EF Core; se acepta el trade-off documentado aqui.
        // Filtrado a filas activas: sin esto, reutilizar el nombre de un proyecto ya borrado
        // logicamente violaria la restriccion aunque la app lo permita.
        builder.HasIndex(p => p.Name, "ix_projects_name_unique")
            .IsUnique()
            .HasFilter("NOT is_deleted");
    }
}
