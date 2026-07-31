using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "Id", "correo", "nombre", "password_hash" },
                values: new object[,]
                {
                    { new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), "admin@ideasgroup.test", "Administrador", "$2a$11$YJ0PQ4j9uGPeu.c0KarD3.nWP8.o7KjhuJ8P/W6JxT4vXAKvumGhu" },
                    { new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), "evaluador@ideasgroup.test", "Evaluador", "$2a$11$YJ0PQ4j9uGPeu.c0KarD3.nWP8.o7KjhuJ8P/W6JxT4vXAKvumGhu" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                table: "usuarios",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
