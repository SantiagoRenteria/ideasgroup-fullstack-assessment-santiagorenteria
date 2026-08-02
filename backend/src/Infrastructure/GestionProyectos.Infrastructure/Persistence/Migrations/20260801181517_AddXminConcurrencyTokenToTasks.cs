using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    // Up/Down vacios: xmin ya existe como columna de sistema de Postgres, solo deja
    // constancia en el historial (ver TaskEntityConfiguration, ADR §15.2).
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
