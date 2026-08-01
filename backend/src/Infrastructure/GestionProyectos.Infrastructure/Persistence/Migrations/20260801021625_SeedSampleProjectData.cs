using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleProjectData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "projects",
                columns: new[] { "Id", "deleted_at", "description", "end_date", "is_deleted", "name", "start_date", "status" },
                values: new object[] { new Guid("d0000000-0000-0000-0000-000000000001"), null, "Proyecto de ejemplo precargado por la migracion semilla para probar el tablero, el tiempo real y los reportes sin crear datos manualmente.", new DateOnly(2026, 12, 31), false, "Proyecto Demo", new DateOnly(2026, 7, 1), 1 });

            migrationBuilder.InsertData(
                table: "columns",
                columns: new[] { "Id", "deleted_at", "is_deleted", "name", "order", "project_id" },
                values: new object[,]
                {
                    { new Guid("d1000000-0000-0000-0000-000000000001"), null, false, "Por hacer", 0, new Guid("d0000000-0000-0000-0000-000000000001") },
                    { new Guid("d1000000-0000-0000-0000-000000000002"), null, false, "En progreso", 1, new Guid("d0000000-0000-0000-0000-000000000001") },
                    { new Guid("d1000000-0000-0000-0000-000000000003"), null, false, "Hecho", 2, new Guid("d0000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "tasks",
                columns: new[] { "Id", "assignee_id", "column_id", "created_at", "deleted_at", "description", "is_deleted", "order", "priority", "title" },
                values: new object[,]
                {
                    { new Guid("d2000000-0000-0000-0000-000000000001"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("d1000000-0000-0000-0000-000000000001"), new DateTime(2026, 7, 2, 9, 0, 0, 0, DateTimeKind.Utc), null, "Bocetos iniciales de las pantallas principales del tablero.", false, "m", 2, "Diseñar wireframes" },
                    { new Guid("d2000000-0000-0000-0000-000000000002"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("d1000000-0000-0000-0000-000000000001"), new DateTime(2026, 7, 3, 9, 0, 0, 0, DateTimeKind.Utc), null, "Listar las funcionalidades minimas para el primer release.", false, "t", 1, "Definir alcance del MVP" },
                    { new Guid("d2000000-0000-0000-0000-000000000003"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("d1000000-0000-0000-0000-000000000002"), new DateTime(2026, 7, 5, 9, 0, 0, 0, DateTimeKind.Utc), null, "JWT, guardia de ruta e interceptor con manejo de 401.", false, "m", 3, "Implementar login" },
                    { new Guid("d2000000-0000-0000-0000-000000000004"), null, new Guid("d1000000-0000-0000-0000-000000000002"), new DateTime(2026, 7, 8, 9, 0, 0, 0, DateTimeKind.Utc), null, "Paginacion y filtro por nombre resuelto en el servidor.", false, "t", 2, "CRUD de proyectos" },
                    { new Guid("d2000000-0000-0000-0000-000000000005"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("d1000000-0000-0000-0000-000000000003"), new DateTime(2026, 7, 1, 9, 0, 0, 0, DateTimeKind.Utc), null, "Arquitectura hexagonal, Angular con Sakai y docker-compose.", false, "m", 0, "Cimientos del proyecto" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tasks",
                keyColumn: "Id",
                keyValue: new Guid("d2000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "tasks",
                keyColumn: "Id",
                keyValue: new Guid("d2000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "tasks",
                keyColumn: "Id",
                keyValue: new Guid("d2000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "tasks",
                keyColumn: "Id",
                keyValue: new Guid("d2000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "tasks",
                keyColumn: "Id",
                keyValue: new Guid("d2000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("d1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "projects",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000001"));
        }
    }
}
