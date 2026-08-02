using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminSeedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"),
                column: "name",
                value: "Luis Rentería");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"),
                column: "name",
                value: "Administrador");
        }
    }
}
