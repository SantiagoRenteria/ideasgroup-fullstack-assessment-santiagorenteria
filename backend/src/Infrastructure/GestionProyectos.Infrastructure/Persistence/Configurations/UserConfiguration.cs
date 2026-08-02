using GestionProyectos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionProyectos.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    // Hash BCrypt de "IdeasGroup2026!" pre-pimentado (HMACSHA256) con el pepper por defecto
    // de .env.example (PASSWORD_PEPPER). Si cambias PASSWORD_PEPPER en un entorno donde ya
    // corriste esta migracion, estos 2 usuarios semilla dejan de poder loguearse: hay que
    // regenerar el hash (ver docs/decisions/arquitectura-decisiones.md) y crear una nueva
    // migracion. Documentado tambien en el README.
    private const string SeedPasswordHash = "$2a$11$YJ0PQ4j9uGPeu.c0KarD3.nWP8.o7KjhuJ8P/W6JxT4vXAKvumGhu";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        builder.HasData(
            new User(
                Guid.Parse("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"),
                "Luis Rentería",
                "luis.renteria@ideasgroup.test",
                SeedPasswordHash),
            new User(
                Guid.Parse("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"),
                "Evaluador",
                "evaluador@ideasgroup.test",
                SeedPasswordHash));
    }
}
