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

        builder.HasOne<Project>()
            .WithMany(p => p.Columns)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Apoya tanto el listado de columnas por proyecto ya ordenadas como el
        // renderizado del tablero en Fase 3 (ix_columns_project_id_order).
        builder.HasIndex(c => new { c.ProjectId, c.Order })
            .HasDatabaseName("ix_columns_project_id_order");
    }
}
