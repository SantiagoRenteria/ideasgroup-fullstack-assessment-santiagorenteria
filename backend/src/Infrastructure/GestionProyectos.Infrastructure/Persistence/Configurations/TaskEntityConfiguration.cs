using GestionProyectos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

// Solo schema en Fase 2: habilita la regla "no borrar columna con tareas" (seccion 6.4)
// contra una tabla real. El Application layer completo de tareas (Commands/Queries,
// calculo LexoRank de Order) es Fase 3 -- ver docs/fases-implementacion.md.
public class TaskEntityConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ColumnId)
            .HasColumnName("column_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasColumnName("priority")
            .IsRequired();

        builder.Property(t => t.AssigneeId)
            .HasColumnName("assignee_id");

        builder.Property(t => t.Order)
            .HasColumnName("order")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(t => !t.IsDeleted);

        // Restrict: mismo motivo que en ColumnConfiguration -- con soft delete un
        // DELETE fisico sobre columns nunca deberia ocurrir desde la app.
        builder.HasOne<Column>()
            .WithMany()
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.ColumnId, t.Order })
            .HasDatabaseName("ix_tasks_column_id_order");
    }
}
