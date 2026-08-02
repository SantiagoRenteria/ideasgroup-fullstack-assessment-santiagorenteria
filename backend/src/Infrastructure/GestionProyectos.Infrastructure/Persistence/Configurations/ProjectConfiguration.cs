using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
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

        // DateRange es derivada de StartDate/EndDate, no una columna propia -- ComplexProperty
        // (EF Core 8) no soporta HasData todavia (dotnet/efcore#31254) y el seed migration
        // (seccion 6.2 del enunciado) lo exige. Ver arquitectura-decisiones.md §22.
        builder.Ignore(p => p.DateRange);

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

        // Nombre explicito en HasIndex (dos indices sobre la misma columna): EF identifica
        // un indice por su lista de propiedades, sin el nombre la segunda llamada
        // reconfigura la MISMA entrada en vez de crear una nueva.

        // GIN de pg_trgm para ILIKE '%texto%' (seccion 6.3, ADR §9); la extension se
        // habilita en AppDbContext.OnModelCreating.
        builder.HasIndex(p => p.Name, "ix_projects_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Defensa en profundidad (la validacion real vive en los CommandHandlers);
        // case-sensitive porque EF no soporta indice por expresion lower(name).
        builder.HasIndex(p => p.Name, "ix_projects_name_unique")
            .IsUnique()
            .HasFilter("NOT is_deleted");

        // Seed data (ADR §9): proyecto de ejemplo para que el evaluador vea tablero, tiempo
        // real y reportes sin crear datos a mano. Fechas fijas para que sea determinista.
        builder.HasData(
            new Project(
                Guid.Parse("d0000000-0000-0000-0000-000000000001"),
                "Proyecto Demo",
                "Proyecto de ejemplo precargado por la migracion semilla para probar el tablero, el tiempo real y los reportes sin crear datos manualmente.",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                ProjectStatus.InProgress));
    }
}
