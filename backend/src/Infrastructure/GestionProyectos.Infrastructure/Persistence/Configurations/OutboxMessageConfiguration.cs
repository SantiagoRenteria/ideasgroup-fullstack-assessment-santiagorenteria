using GestionProyectos.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(m => m.ExcludeConnectionId)
            .HasColumnName("exclude_connection_id")
            .HasMaxLength(100);

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(m => m.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        // El dispatcher solo consulta pendientes ordenados por creacion
        // (WHERE processed_at_utc IS NULL ORDER BY created_at_utc) -- el indice cubre
        // exactamente ese acceso.
        builder.HasIndex(m => new { m.ProcessedAtUtc, m.CreatedAtUtc })
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
