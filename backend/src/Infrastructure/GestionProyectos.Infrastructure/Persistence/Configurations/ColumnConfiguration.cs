using GestionProyectos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

public class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        builder.ToTable("columns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(c => !c.IsDeleted);

        // Restrict, no Cascade: con soft delete la app nunca emite un DELETE fisico sobre
        // projects, asi que uno aqui seria un bug -- Restrict lo bloquea a nivel de BD.
        // Sin navegacion en Project (WithMany() vacio): Project y Column son agregados
        // independientes, no un arbol navegable de un solo agregado -- ver
        // arquitectura-decisiones.md §22.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Apoya tanto el listado de columnas por proyecto ya ordenadas como el
        // renderizado del tablero en Fase 3 (ix_columns_project_id_order).
        builder.HasIndex(c => new { c.ProjectId, c.Order })
            .HasDatabaseName("ix_columns_project_id_order");

        // Columnas del proyecto de ejemplo (ver ProjectConfiguration.HasData).
        builder.HasData(
            new Column(Guid.Parse("d1000000-0000-0000-0000-000000000001"), Guid.Parse("d0000000-0000-0000-0000-000000000001"), "Por hacer", 0),
            new Column(Guid.Parse("d1000000-0000-0000-0000-000000000002"), Guid.Parse("d0000000-0000-0000-0000-000000000001"), "En progreso", 1),
            new Column(Guid.Parse("d1000000-0000-0000-0000-000000000003"), Guid.Parse("d0000000-0000-0000-0000-000000000001"), "Hecho", 2));
    }
}
