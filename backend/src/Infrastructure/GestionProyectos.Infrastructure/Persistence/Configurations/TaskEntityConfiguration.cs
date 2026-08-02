using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

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

        // Value Object LexoRankKey (arquitectura-decisiones.md §22): antes cualquier string
        // no vacio pasaba como Order valido; el VO ahora valida tambien el alfabeto base62.
        builder.Property(t => t.Order)
            .HasConversion(order => order.Value, value => new LexoRankKey(value))
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
        // Sin navegacion en Column: Column y TaskEntity son agregados independientes
        // -- ver arquitectura-decisiones.md §22.
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

        // Concurrencia optimista con xmin; el helper "obsoleto" se mantiene pese al warning
        // porque el reemplazo sugerido genera una migracion invalida (ADR §15.2).
#pragma warning disable CS0618
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Tareas del proyecto de ejemplo (ver ProjectConfiguration.HasData); Order son
        // claves LexoRank provisorias, el algoritmo real llega en Fase 3.
        var admin = Guid.Parse("6f9b1c2e-1a2b-4c3d-8e4f-000000000001");
        var evaluador = Guid.Parse("6f9b1c2e-1a2b-4c3d-8e4f-000000000002");
        var porHacer = Guid.Parse("d1000000-0000-0000-0000-000000000001");
        var enProgreso = Guid.Parse("d1000000-0000-0000-0000-000000000002");
        var hecho = Guid.Parse("d1000000-0000-0000-0000-000000000003");

        builder.HasData(
            new TaskEntity(
                Guid.Parse("d2000000-0000-0000-0000-000000000001"), porHacer,
                "Diseñar wireframes", "Bocetos iniciales de las pantallas principales del tablero.",
                TaskPriority.High, admin, "m", new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc)),
            new TaskEntity(
                Guid.Parse("d2000000-0000-0000-0000-000000000002"), porHacer,
                "Definir alcance del MVP", "Listar las funcionalidades minimas para el primer release.",
                TaskPriority.Medium, evaluador, "t", new DateTime(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc)),
            new TaskEntity(
                Guid.Parse("d2000000-0000-0000-0000-000000000003"), enProgreso,
                "Implementar login", "JWT, guardia de ruta e interceptor con manejo de 401.",
                TaskPriority.Urgent, admin, "m", new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc)),
            new TaskEntity(
                Guid.Parse("d2000000-0000-0000-0000-000000000004"), enProgreso,
                "CRUD de proyectos", "Paginacion y filtro por nombre resuelto en el servidor.",
                TaskPriority.High, null, "t", new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc)),
            new TaskEntity(
                Guid.Parse("d2000000-0000-0000-0000-000000000005"), hecho,
                "Cimientos del proyecto", "Arquitectura hexagonal, Angular con Sakai y docker-compose.",
                TaskPriority.Low, evaluador, "m", new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)));
    }
}
