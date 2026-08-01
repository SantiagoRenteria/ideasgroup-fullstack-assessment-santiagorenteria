# GestiónProyectos — Prueba Técnica IDEASGROUP-REM-LAT-26-2907

Aplicativo web para la gestión de proyectos ágiles: proyectos, columnas configurables y tablero kanban con tiempo real, sobre .NET 8 (arquitectura hexagonal) + Angular 17 (PrimeNG/Sakai) + PostgreSQL.

> Documento vivo, se actualiza en paralelo al desarrollo (no se escribe al final). Última actualización: 2026-08-01, al cierre de la Fase 2.

---

## Índice

1. [Estado del proyecto](#1-estado-del-proyecto)
2. [Instrucciones de ejecución](#2-instrucciones-de-ejecución)
3. [Stack tecnológico](#3-stack-tecnológico)
4. [Arquitectura](#4-arquitectura)
5. [Decisiones arquitectónicas](#5-decisiones-arquitectónicas)
6. [Tiempo real](#6-tiempo-real)
7. [Estrategia de ordenamiento](#7-estrategia-de-ordenamiento)
8. [Exportación dual (PDF/Excel)](#8-exportación-dual-pdfexcel)
9. [Diagrama de base de datos](#9-diagrama-de-base-de-datos)
10. [Pruebas automatizadas](#10-pruebas-automatizadas)
11. [Uso de inteligencia artificial](#11-uso-de-inteligencia-artificial)

---

## 1. Estado del proyecto

Plan completo de 7 días en `docs/fases-implementacion.md`. Estado actual:

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Cimientos (hexagonal, Angular+Sakai, docker-compose) | ✅ Completa |
| 1 | Autenticación JWT, hash salt+pepper, guard, interceptor | ✅ Completa |
| 2 | CRUD de Proyectos y Columnas, paginación/filtro, soft-delete | ✅ Completa |
| 3 | Tablero kanban, tareas, drag&drop, cálculo de posición (LexoRank) | ⏳ Pendiente |
| 4 | Tiempo real (SignalR) | ⏳ Pendiente |
| 5 | Reportes duales (PDF/Excel) | ⏳ Pendiente |
| 6 | Pruebas restantes, diagrama ERD, opcionales | ⏳ Pendiente |

Este README se actualiza al cierre de cada fase — las secciones marcadas "Pendiente" abajo reflejan el diseño ya decidido (documentado en `docs/decisions/arquitectura-decisiones.md`), no lo implementado todavía.

---

## 2. Instrucciones de ejecución

Requiere Docker y Docker Compose. No requiere instalar .NET, Node ni PostgreSQL localmente.

```bash
git clone https://github.com/SantiagoRenteria/ideasgroup-fullstack-assessment-santiagorenteria.git
cd ideasgroup-fullstack-assessment-santiagorenteria
cp .env.example .env
docker compose up --build
```

El `.env.example` ya trae valores por defecto funcionales para un entorno de evaluación local (no son secretos reales — ver advertencia dentro del archivo). Las migraciones de EF Core corren automáticamente al arrancar la API, incluyendo la migración semilla con los 2 usuarios de prueba.

**Servicios y puertos** (configurables en `.env`):

| Servicio | URL | Notas |
|---|---|---|
| Frontend (Angular) | http://localhost:4200 | |
| API (.NET 8) | http://localhost:5000 | |
| Swagger UI | http://localhost:5000/swagger | Botón "Authorize" para pegar el JWT y probar endpoints protegidos |
| PostgreSQL | localhost:5432 | |

**Usuarios semilla** (migración `InitialCreate`):

| Correo | Contraseña |
|---|---|
| `admin@ideasgroup.test` | `IdeasGroup2026!` |
| `evaluador@ideasgroup.test` | `IdeasGroup2026!` |

**Colección de Postman**: `postman/GestionProyectos.postman_collection.json` + `postman/GestionProyectos.postman_environment.json` (environment "GestionProyectos - Local"). Cubre Auth, Projects y Columns con happy path y casos de error (401/404/409), encadenando automáticamente los IDs creados.

---

## 3. Stack tecnológico

| Componente | Tecnología |
|---|---|
| Backend | .NET 8, C#, Minimal API |
| Frontend | Angular 17, TypeScript, SCSS, PrimeNG (plantilla Sakai) |
| Persistencia | Entity Framework Core, migraciones incrementales |
| Base de datos | PostgreSQL 16 |
| CQRS / Mediator | MediatR |
| Validación | FluentValidation |
| Autenticación | JWT (HS256), BCrypt + pepper |
| Contenedores | Docker Compose (Postgres, API, SPA con nginx) |
| Reporte PDF | QuestPDF *(Fase 5, pendiente)* |
| Reporte Excel | ClosedXML *(Fase 5, pendiente)* |
| Tiempo real | SignalR *(Fase 4, pendiente)* |

Detalle completo y alternativas descartadas: `docs/decisions/arquitectura-decisiones.md` §1.

---

## 4. Arquitectura

**Backend — Hexagonal (puertos y adaptadores)**, exigida explícitamente por el enunciado:

```
Domain/          Entidades y reglas de negocio puras, sin dependencias externas
Application/     Casos de uso (Commands/Queries vía MediatR), puertos, DTOs, validadores
Infrastructure/  Adaptadores: EF Core, seguridad (JWT/BCrypt)
API/             Adaptador de entrada HTTP (Minimal API endpoints), middlewares
```

Dentro de `Application/`, cada feature (`Projects/`, `Columns/`) separa físicamente `Commands/` de `Queries/`, con una subcarpeta por operación — la separación de CQRS es visible en la carpeta, no solo en el sufijo del nombre de clase (ver `docs/METODOLOGIA.md` §7.1).

**Frontend — organización por capas** (`core/shared/features`), la alternativa que el enunciado permite explícitamente frente a hexagonal literal en el SPA:

```
core/              Servicios singleton, guards, interceptors, modelos transversales (auth)
features/{feature}/  Módulos por dominio, lazy-loaded (ej. features/projects/)
```

Justificación completa: `docs/decisions/arquitectura-decisiones.md` §2.

---

## 5. Decisiones arquitectónicas

El detalle completo de cada decisión, sus alternativas evaluadas y por qué se descartaron vive en **`docs/decisions/arquitectura-decisiones.md`** — es el material de sustentación técnica, no un resumen aquí duplicado. Incluye, entre otras:

- Por qué Minimal API en vez de Controllers.
- Por qué Repository + Unit of Work sobre `DbContext` directo.
- BCrypt sobre Argon2, y el mecanismo de pepper (HMACSHA256 previo al hash).
- Por qué el JWT vive en memoria en el cliente, no en `localStorage` ni cookie.
- Dos decisiones revertidas y documentadas como tal (no reescritas): identificadores de código en inglés (§12) y borrado de Proyecto — de hard delete en cascada a soft-delete + regla "no borrar con tareas" (§13).

---

## 6. Tiempo real

**Pendiente (Fase 4).** Decisión ya tomada: **SignalR**, con grupos por `boardId`/`proyectoId`, canal autenticado con el mismo JWT de sesión. Alternativas descartadas (WebSocket crudo, SSE) y justificación completa: ADR §1 y §2.

---

## 7. Estrategia de ordenamiento

**Pendiente (Fase 3).** Decisión ya tomada: claves ordenables tipo string (**LexoRank simplificado**, base36/base62) para el orden de tareas dentro de una columna, con rebalanceo cuando el espacio entre dos claves se agota. Para columnas dentro de un proyecto (ya implementado en Fase 2) se usa un `int Order` simple — no requiere LexoRank porque las columnas se reordenan con baja frecuencia, a diferencia de las tareas en un tablero activo.

Alternativas descartadas (índice entero secuencial, `float`, lista enlazada) y detalle del algoritmo: ADR §4.

---

## 8. Exportación dual (PDF/Excel)

**Pendiente (Fase 5).** Diseño ya definido: un DTO común (`ProjectReportDto`) y una sola consulta EF alimentan ambos formatos; `IReportExporter` como puerto, con `QuestPdfReportExporter` y `ClosedXmlReportExporter` resueltos por inyección de `IEnumerable<IReportExporter>` — agregar un tercer formato no requiere tocar el endpoint ni las clases existentes. Detalle: ADR §5.

---

## 9. Diagrama de base de datos

**Pendiente.** Se genera como imagen PNG desde el esquema real de las migraciones una vez que el modelo de `Task` esté estable (cierre de Fase 3) — generarlo antes implicaría rehacerlo cuando se agregue el Application layer completo de tareas.

Esquema actual (Fase 0-2): `users`, `projects`, `columns`, `tasks` (schema mínimo, sin CRUD todavía). Migraciones incrementales en `backend/src/Infrastructure/GestionProyectos.Infrastructure/Persistence/Migrations/`.

---

## 10. Pruebas automatizadas

| Capa | Cantidad | Cobertura |
|---|---|---|
| Backend (xUnit) | 60 | Domain (entidades, validaciones, soft-delete), Application (handlers CQRS con NSubstitute, incluida la regla "no borrar con tareas"), Infrastructure (BCrypt, JWT) |
| Frontend (Jasmine/Karma) | 8 | `ProjectService`, `ColumnService` |

Mínimo exigido por el enunciado (sección 6.9): 5 backend + 5 frontend. Superado en backend; frontend cumple el mínimo pero con cobertura angosta (solo servicios) — se amplía a componentes y a `auth` (Fase 1 nunca tuvo tests) como parte del trabajo en curso.

La prueba unitaria del cálculo de posición al reordenar (única exigida por nombre, sección 6.9) llega con el algoritmo LexoRank en Fase 3, vía TDD.

---

## 11. Uso de inteligencia artificial

Se utilizó **Claude Code** (Anthropic) como asistente de desarrollo durante todo el proyecto, bajo un contrato de comportamiento explícito (`CLAUDE.md`, no versionado — define cómo se comporta la IA, nunca contiene decisiones de negocio) y una metodología documentada (`docs/METODOLOGIA.md`).

Partes del desarrollo donde se usó:

- **Diseño y documentación de arquitectura**: registro de decisiones y alternativas descartadas en `docs/decisions/arquitectura-decisiones.md`, incluyendo dos revisiones de decisiones ya tomadas (idioma de identificadores, política de borrado) documentadas como tales, no reescritas.
- **Implementación de código**: entidades de dominio, capa de aplicación (CQRS/MediatR), configuraciones de EF Core y migraciones, endpoints de Minimal API, componentes Angular.
- **Pruebas unitarias**: backend (xUnit + NSubstitute) y frontend (Jasmine/Karma), incluyendo casos borde más allá del mínimo exigido.
- **Refactors dirigidos**: retrofit de nomenclatura de dominio al inglés, reestructuración de Application por Commands/Queries, corrección de un bug real de la API (filtro `status` vacío) detectado al construir y correr la colección de Postman con Newman.
- **Revisión crítica**: el asistente fue instruido para cuestionar decisiones (no solo ejecutar pedidos) y documentar el porqué de cada cambio de rumbo — visible en las secciones "Decisión superada" del ADR.

Toda decisión de arquitectura o de negocio fue revisada y aprobada explícitamente por el desarrollador antes de implementarse; el asistente no tomó decisiones de diseño de forma autónoma.
