using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFinalProjectData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"),
                column: "email",
                value: "luis.renteria@ideasgroup.test");

            // Reemplaza el seed generico de SeedSampleProjectData (2026-08-01) por uno que usa
            // como datos de demostracion el propio desarrollo de esta prueba tecnica -- mas
            // creible para el video (#27) y la sustentacion que un "Proyecto Demo" generico.
            // No se edita esa migracion ya aplicada (regla de METODOLOGIA.md): se revierte su
            // seed y se inserta uno nuevo, igual que se haria en cualquier migracion posterior.
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

            migrationBuilder.InsertData(
                table: "projects",
                columns: new[] { "Id", "deleted_at", "description", "end_date", "is_deleted", "name", "start_date", "status" },
                values: new object[] { new Guid("e0000000-0000-0000-0000-000000000001"), null, "El propio desarrollo de esta prueba tecnica (IDEASGROUP-REM-LAT-26-2907), usado como proyecto de demostracion: cada tarea es una fase o issue real ya resuelta (o en curso) durante el reto.", new DateOnly(2026, 8, 5), false, "Prueba Tecnica IdeasGroup", new DateOnly(2026, 7, 30), 1 });

            migrationBuilder.InsertData(
                table: "columns",
                columns: new[] { "Id", "deleted_at", "is_deleted", "name", "order", "project_id" },
                values: new object[,]
                {
                    { new Guid("e1000000-0000-0000-0000-000000000001"), null, false, "Backlog", 0, new Guid("e0000000-0000-0000-0000-000000000001") },
                    { new Guid("e1000000-0000-0000-0000-000000000002"), null, false, "En progreso", 1, new Guid("e0000000-0000-0000-0000-000000000001") },
                    { new Guid("e1000000-0000-0000-0000-000000000003"), null, false, "Review", 2, new Guid("e0000000-0000-0000-0000-000000000001") },
                    { new Guid("e1000000-0000-0000-0000-000000000004"), null, false, "Done", 3, new Guid("e0000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "tasks",
                columns: new[] { "Id", "assignee_id", "column_id", "created_at", "deleted_at", "description", "is_deleted", "order", "priority", "title" },
                values: new object[,]
                {
                    { new Guid("e2000000-0000-0000-0000-000000000001"), null, new Guid("e1000000-0000-0000-0000-000000000001"), new DateTime(2026, 8, 2, 18, 0, 0, 0, DateTimeKind.Utc), null, "Idea para una futura iteracion mas alla del alcance de la prueba tecnica: permisos granulares (lector/editor) por proyecto.", false, "a", 0, "Roles y permisos por usuario (idea futura)" },
                    { new Guid("e2000000-0000-0000-0000-000000000002"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000002"), new DateTime(2026, 8, 2, 17, 25, 0, 0, DateTimeKind.Utc), null, "Guion ya preparado: login, CRUD, tablero en tiempo real con dos sesiones, filtros/busqueda, descarga de ambos reportes. Falta grabar y subir a un enlace publico.", false, "a", 3, "#27 Grabar video demostrativo" },
                    { new Guid("e2000000-0000-0000-0000-000000000003"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000003"), new DateTime(2026, 8, 2, 17, 30, 0, 0, DateTimeKind.Utc), null, "Banner PrimeBlocks, configurador de tema completo, dependencias sin uso (chart.js/fullcalendar/quill/prismjs) y mensajes de error reales en los toasts.", false, "a", 1, "#47 Pulido final de UI y limpieza de residuos de Sakai" },
                    { new Guid("e2000000-0000-0000-0000-000000000004"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000003"), new DateTime(2026, 8, 2, 17, 35, 0, 0, DateTimeKind.Utc), null, "Reemplaza el seed generico anterior por este mismo proyecto, usado como datos de demostracion para el video (#27).", false, "b", 0, "#48 Seed final con datos reales de la prueba tecnica" },
                    { new Guid("e2000000-0000-0000-0000-000000000005"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 7, 30, 20, 0, 0, 0, DateTimeKind.Utc), null, "Arquitectura hexagonal en el backend, Angular con plantilla Sakai, docker-compose con Postgres/API/SPA.", false, "a", 1, "Fase 0: Cimientos (hexagonal + Angular/Sakai + docker-compose)" },
                    { new Guid("e2000000-0000-0000-0000-000000000006"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 7, 31, 22, 0, 0, 0, DateTimeKind.Utc), null, "Hash con salt+pepper, guardia de ruta e interceptor con manejo de 401.", false, "b", 2, "Fase 1: Autenticacion JWT (BCrypt + pepper, guard, interceptor)" },
                    { new Guid("e2000000-0000-0000-0000-000000000007"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, "Paginacion y filtro por nombre resueltos en el servidor, soft-delete.", false, "c", 1, "Fase 2: CRUD de Proyectos y Columnas" },
                    { new Guid("e2000000-0000-0000-0000-000000000008"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 1, 17, 0, 0, 0, DateTimeKind.Utc), null, "Drag & drop entre columnas, calculo de posicion con LexoRank simplificado y rebalanceo.", false, "d", 3, "Fase 3: Tablero Kanban y ordenamiento LexoRank" },
                    { new Guid("e2000000-0000-0000-0000-000000000009"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 0, 15, 0, 0, DateTimeKind.Utc), null, "Notificacion de cambios del tablero entre sesiones, concurrencia optimista con xmin.", false, "e", 2, "Fase 4: Tiempo real con SignalR" },
                    { new Guid("e2000000-0000-0000-0000-000000000010"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 1, 15, 0, 0, DateTimeKind.Utc), null, "QuestPDF y ClosedXML sobre una misma consulta y DTO comunes.", false, "f", 1, "Fase 5: Reportes duales PDF/Excel (#16-#19)" },
                    { new Guid("e2000000-0000-0000-0000-000000000011"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 1, 40, 0, 0, DateTimeKind.Utc), null, "Cobertura de handlers sin test, ERD por introspeccion real, README verificado en entorno limpio.", false, "g", 1, "Fase 6: Pruebas restantes, ERD y README (#20-#22)" },
                    { new Guid("e2000000-0000-0000-0000-000000000012"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 2, 5, 0, 0, DateTimeKind.Utc), null, "Filtro client-side en el tablero, mismo filtro enviado tambien al reporte.", false, "h", 0, "#23 Filtros por responsable y prioridad" },
                    { new Guid("e2000000-0000-0000-0000-000000000013"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 2, 20, 0, 0, DateTimeKind.Utc), null, "IBoardPresenceTracker en memoria + BoardHub actualizado, verificado con dos sesiones reales.", false, "i", 0, "#24 Indicador de presencia en el tablero" },
                    { new Guid("e2000000-0000-0000-0000-000000000014"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 2, 28, 0, 0, DateTimeKind.Utc), null, "Reutiliza el mecanismo de filtros ya construido en #23.", false, "j", 0, "#25 Busqueda de tareas por texto" },
                    { new Guid("e2000000-0000-0000-0000-000000000015"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 14, 55, 0, 0, DateTimeKind.Utc), null, "CorsOptions con whitelist explicita + RateLimiting (5/min por IP en login).", false, "k", 2, "#26 CORS whitelist y rate limiting en login" },
                    { new Guid("e2000000-0000-0000-0000-000000000016"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 15, 8, 0, 0, DateTimeKind.Utc), null, "Se eliminaron ~264 archivos demo de la plantilla PrimeNG Sakai.", false, "l", 1, "#38 Limpiar plantilla Sakai" },
                    { new Guid("e2000000-0000-0000-0000-000000000017"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000002"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 15, 28, 0, 0, DateTimeKind.Utc), null, "ILogger<T> en LoginCommandHandler y los 4 handlers de Tasks, UserId via LogContext.PushProperty.", false, "m", 1, "#37 ILogger real en handlers criticos" },
                    { new Guid("e2000000-0000-0000-0000-000000000018"), new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"), new Guid("e1000000-0000-0000-0000-000000000004"), new DateTime(2026, 8, 2, 17, 15, 0, 0, DateTimeKind.Utc), null, "GitHub Actions: build+test backend (.NET 8) y frontend (Angular 17) en cada push/PR.", false, "n", 0, "#35 CI minimo con GitHub Actions" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var i = 1; i <= 18; i++)
            {
                migrationBuilder.DeleteData(
                    table: "tasks",
                    keyColumn: "Id",
                    keyValue: new Guid($"e2000000-0000-0000-0000-{i:D12}"));
            }

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "columns",
                keyColumn: "Id",
                keyValue: new Guid("e1000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "projects",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0000-0000-0000-000000000001"));

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

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("6f9b1c2e-1a2b-4c3d-8e4f-000000000001"),
                column: "email",
                value: "admin@ideasgroup.test");
        }
    }
}
