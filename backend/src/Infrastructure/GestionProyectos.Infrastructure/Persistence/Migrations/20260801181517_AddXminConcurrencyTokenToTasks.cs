using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    // Up/Down vacios a proposito: "xmin" ya existe fisicamente en toda tabla de Postgres
    // (columna de sistema), asi que el AddColumn que el scaffolding genero por defecto
    // fallaria en runtime ("column name xmin conflicts with a system column name"). Esta
    // migracion solo deja constancia en el historial de que TaskEntity empezo a mapear
    // xmin como token de concurrencia (ver TaskEntityConfiguration y
    // docs/decisions/arquitectura-decisiones.md §15.2) -- no cambia el esquema.
    /// <inheritdoc />
    public partial class AddXminConcurrencyTokenToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
