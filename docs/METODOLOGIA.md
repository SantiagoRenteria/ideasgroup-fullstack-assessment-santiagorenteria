# Metodología del Proyecto

Este documento define **cómo trabajamos**. `CLAUDE.md` define cómo se comporta la IA; este documento define el proceso que gobierna ambos. Es de obligado cumplimiento durante los 7 días del reto — se actualiza si un supuesto cambia, nunca se ignora en silencio.

Índice: [1. Reglas del proyecto](#1-reglas-del-proyecto) · [2. Metodología](#2-metodología) · [3. Roles](#3-roles) · [4. Flujo Git](#4-flujo-git) · [5. Gestión de Issues](#5-gestión-de-issues) · [6. Gestión de documentación](#6-gestión-de-documentación) · [7. Convenciones](#7-convenciones) · [8. Flujo de trabajo diario](#8-flujo-de-trabajo-diario) · [9. Checklist de calidad](#9-checklist-de-calidad)

---

## 1. Reglas del proyecto

Restricciones no negociables, tomadas directamente del enunciado (proceso IDEASGROUP-REM-LAT-26-2907). No son "buenas prácticas sugeridas" — son criterio de evaluación.

| Regla | Origen | Consecuencia de incumplir |
|---|---|---|
| Stack fijo: .NET 8, Angular 17, PrimeNG+Sakai, PostgreSQL, EF Core, QuestPDF | Sección 4 | Desviarse no es "criterio libre", es no seguir el enunciado |
| Arquitectura Hexagonal en backend (Domain/Application/Infrastructure) | Secciones 4, 6.1 | Penaliza "criterio arquitectónico" (parte de la sustentación) |
| Modelo de dominio mínimo (Usuario, Proyecto, Columna, Tarea) — puede ampliarse, no reducirse | Sección 5 | Requisito obligatorio |
| Commits atómicos, descriptivos, **distribuidos a lo largo de los 6 días** | Sección 9 | Entrega concentrada en 1-2 commits masivos se penaliza explícitamente |
| Declarar en el README qué IA se usó y en qué partes | Sección 9 | Obligatorio, no opcional |
| Config externa (env vars), sin secretos ni connection strings versionados | Sección 6.1 | Requisito obligatorio de seguridad |
| Mínimo 5 pruebas unitarias backend + 5 frontend; una debe cubrir el cálculo de posición al reordenar | Sección 6.9 | Único test nombrado explícitamente — no es opcional |
| README con: instrucciones de ejecución, decisiones arquitectónicas justificadas, tecnología de tiempo real y alternativas descartadas, estrategia de índices de ordenamiento, patrón de exportación dual, declaración de IA | Sección 8 | Entregable obligatorio |
| Diagrama ERD como **imagen embebida** en el README (no texto/esquema de herramienta externa) | Sección 8 | Requisito de formato explícito |
| El PDF del enunciado es confidencial — no se publica ni difunde mientras el proceso esté en curso | Sección 12 | Nunca commitear el PDF al repo público |
| Los entregables (URL del repo, README, ERD, video si aplica) deben **enviarse por correo** a la dirección donde se recibió el ejercicio, para trazabilidad | Sección 8 (nota) | Un repo perfecto sin el correo de entrega no queda registrado como entregado |
| Arquitectura por capas o hexagonal también en el **frontend** (no solo backend) | Sección 4 | Ver `docs/decisions/arquitectura-decisiones.md` §2.1 — se cumple con `core/shared/features` |
| El evaluador levanta el proyecto en limpio siguiendo solo el README | Sección 12 | Si no arranca con las instrucciones, no se puede verificar esa parte |
| Toda decisión no especificada en el enunciado queda a criterio del aspirante, pero **debe documentarse en el README** | Sección 9 | Decisión no documentada = decisión no defendible |

**Ponderación** (sección 10): funcionalidades obligatorias 60%, sustentación 40%, video +5%, opcionales +5%. Prioridad de esfuerzo: obligatorio y defendible > opcional.

---

## 2. Metodología

**Kanban personal, no Scrum ceremonial.** Con un solo desarrollador y 7 días, las ceremonias de Scrum (dailies, plannings, retros formales) no aportan valor — se sustituyen por:

- Un **tablero de issues en GitHub** (irónico y apropiado: el propio producto es un tablero Kanban) con columnas `Backlog → En progreso → Review → Done`.
- **TDD dirigido por riesgo**, no TDD dogmático en todo el código: se aplica estrictamente al algoritmo de ordenamiento (única prueba exigida por nombre, sección 6.9) y a las reglas de negocio con casos borde (no borrar columna con tareas, conflicto de concurrencia). El resto del código se prueba donde aporte confianza real, no por cobertura numérica.
- **Fases secuenciales con dependencias explícitas** — ver `docs/fases-implementacion.md`. No se empieza tiempo real antes de que CRUD + tablero estén sólidos, para no depurar dos capas de fallo a la vez.
- **Documentación incremental**, no al final: el README se escribe en paralelo desde el día 1 (decisión de la Fase 6 del plan), porque las decisiones se justifican mejor en caliente que reconstruidas de memoria el día 6.

---

## 3. Roles

No hay equipo humano — Santiago es el único desarrollador. Los "roles" son **perspectivas que la IA adopta según la tarea**, definidos en `CLAUDE.md` § Uso de IA. Tabla de cuándo invocar cada una:

| Rol | Se invoca cuando... | Pregunta que responde |
|---|---|---|
| Product Owner | Hay ambigüedad en un requisito o una decisión no especificada en el enunciado | ¿Qué comportamiento tiene más sentido de negocio? |
| Arquitecto | Antes de crear una entidad, un puerto nuevo, o cruzar una capa hexagonal | ¿Esto respeta Domain ← Application ← Infrastructure ← API? |
| Backend | Implementación en .NET/C# | ¿Es correcto, seguro, testeable? |
| Frontend | Implementación en Angular | ¿Es accesible, reactivo, sin lógica de negocio duplicada del backend? |
| QA | Antes de cerrar cualquier funcionalidad | ¿Qué casos borde faltan? ¿Hay test que lo demuestre? |
| Code Reviewer | Al final de cada rama, antes de mergear a `main` | ¿Yo aprobaría esto si lo viera en un PR de otra persona? |
| DevOps | Docker, variables de entorno, CI | ¿Esto arranca en limpio con solo el README? |

La IA no decide sola cuál rol usar en decisiones de arquitectura — eso pasa por el paso 3 del flujo obligatorio de `CLAUDE.md` (esperar confirmación).

---

## 4. Flujo Git

**Modelo:** trunk-based simplificado — `main` protegida + una rama por funcionalidad, fusionada vía Pull Request (autorrevisado, ver § 9).

### 4.1 Ramas

```
main                        # siempre desplegable, protegida
feature/cimientos           # Fase 0
feature/auth                # Fase 1
feature/projects-columns    # Fase 2
feature/board-ordering      # Fase 3 (incluye TDD del algoritmo)
feature/realtime            # Fase 4
feature/reports             # Fase 5
feature/tests-docs          # Fase 6
```

Una rama = un objetivo = una fase del plan (`docs/fases-implementacion.md`). No se abren ramas para "arreglos rápidos" sueltos — esos van como commits adicionales dentro de la rama activa o, si son post-merge, `fix/<descripcion>`.

> **Nota de transparencia:** las Fases 0 y 1 se desarrollaron con commits directos a `main`, sin rama `feature/*` ni PR — un descuido de proceso detectado al revisar el trabajo, no una decisión consciente. No se reescribió el historial ya pusheado para corregirlo (regla de la sección 4.2) ni se simularon ramas/PR retroactivos sobre commits ya fusionados, porque un PR sin commits reales que fusionar habría sido cosmético, no autogestión real. El flujo rama+PR descrito aquí se aplica de forma estricta desde la Fase 2 en adelante.

### 4.2 Commits — Conventional Commits

```
feat(auth): implement JWT authentication
fix(task): preserve task ordering on concurrent move
docs(readme): explain architecture decisions
test(domain): add reorder position tests
refactor(application): simplify export use case
chore(docker): add postgres healthcheck
```

Reglas:
- Pequeños y atómicos — un commit, un cambio lógico.
- **Nunca acumular cambios de varios días en un commit.** Es la regla que el enunciado penaliza explícitamente (sección 9): commitear al final de cada sesión de trabajo real, no al final del proyecto.
- Nunca cambiar `user.name` / `user.email` configurados.
- Nunca reescribir historial ya pusheado a `main` (no `force-push` a `main`).

### 4.3 Pull Requests

Aunque el desarrollador es uno solo, cada fase cierra con un PR de `feature/*` → `main`, usando `.github/PULL_REQUEST_TEMPLATE.md`. El PR es el punto de auto-revisión formal (rol Code Reviewer) y queda como evidencia de proceso ante el evaluador — no es burocracia, es la forma de demostrar la "capacidad de autogestión" que la sección 2 del enunciado dice que se evalúa.

---

## 5. Gestión de Issues

Una **issue de GitHub por funcionalidad del enunciado**, mapeada 1:1 a las secciones 6.1–6.9 y a los deseables de la sección 7. El campo "milestone" agrupa issues por fase (0–7).

### 5.1 Labels

| Label | Uso |
|---|---|
| `must-have` | Requisito obligatorio (secciones 6.1–6.9) |
| `nice-to-have` | Requisito deseable (sección 7) |
| `bug` | Defecto sobre algo ya implementado |
| `docs` | Documentación (README, ADRs) |
| `test` | Cobertura de pruebas |
| `security` | OWASP / hardening |

### 5.2 Flujo de una issue

`Backlog` (creada, sin empezar) → `En progreso` (rama abierta, `feature/*` referenciando la issue con `Closes #N`) → `Review` (PR abierto) → `Done` (PR mergeado a `main`).

### 5.3 Plantillas

`.github/ISSUE_TEMPLATE/feature.md` y `.github/ISSUE_TEMPLATE/bug.md` — ver § 6 de este documento para su contenido. Mantienen consistencia: sección del enunciado que cubre, criterio de aceptación, y si aplica, alternativas de diseño consideradas.

### 5.4 Backlog inicial

`scripts/setup-github-issues.sh` crea de una vez los labels, los 8 milestones (uno por fase) y todas las issues del backlog obligatorio + deseable, mapeadas 1:1 a las secciones 6.1–6.9 y 7 del enunciado. Requiere `gh auth login` previo. Es idempotente: puede volver a correrse sin duplicar labels.

---

## 6. Gestión de documentación

**Regla general (de `CLAUDE.md`): solo documentación que aporte valor.** No hay documento por el hecho de tener documento.

### 6.1 Estructura

```
/
├── CLAUDE.md                          # contrato de comportamiento de la IA — local, no versionado (ver .gitignore)
├── README.md                          # entregable oficial (sección 8 del enunciado)
├── .gitignore                         # excluye el PDF del enunciado, CLAUDE.md y secretos
├── .env.example
└── docs/
    ├── METODOLOGIA.md                 # este documento
    ├── fases-implementacion.md        # plan de 7 días
    └── decisions/
        └── arquitectura-decisiones.md # ADR-style, vivo, se actualiza no se reescribe
```

### 6.2 Qué va en cada lugar

- **README.md**: lo que exige la sección 8 — instrucciones de ejecución, resumen de decisiones (con link a `docs/decisions/`), tecnología de tiempo real elegida y alternativas descartadas, estrategia de ordenamiento, patrón de exportación dual, diagrama ERD embebido, declaración de uso de IA.
- **docs/decisions/arquitectura-decisiones.md**: el detalle completo de cada decisión y su justificación — el README resume, este documento sustenta. Es el material de estudio para la entrevista de sustentación.
- **CLAUDE.md**: nunca contiene decisiones de negocio o de arquitectura — solo reglas de comportamiento e interacción.
- **Comentarios en código**: solo donde el código no pueda explicarse a sí mismo (por qué, no qué). Se prefiere nombrar bien antes que comentar.

### 6.3 El PDF del enunciado

Vive localmente, fuera de control de versiones (regla de confidencialidad, sección 12). Se referencia por número de sección en todos los documentos versionados, nunca se transcribe completo ni se commitea.

---

## 7. Convenciones

### 7.1 Backend (.NET 8 / C#)

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases | PascalCase singular | `TaskEntity`, `ProjectRepository` |
| Interfaces (puertos) | `I` + PascalCase | `ITaskRepository`, `IReportExporter` |
| Commands | `{Accion}{Entidad}Command` | `CreateProjectCommand`, `MoveTaskCommand` |
| Queries | `Get{Entidad}Query` / `List{Entidad}Query` | `GetProjectByIdQuery` |
| Handlers | `{Command/Query}Handler` | `CreateProjectCommandHandler` |
| DTOs | `{Entidad}{Tipo}Dto` | `ProjectResponseDto`, `TaskCreateDto` |
| Campos privados | `_camelCase` | `_dbContext`, `_repository` |
| Tablas PostgreSQL | `snake_case` plural | `projects`, `tasks`, `columns` |
| Columnas | `snake_case` | `column_id`, `created_at` |
| Índices | `ix_{tabla}_{columnas}` | `ix_tasks_column_id_order` |

### 7.2 Frontend (Angular 17)

| Elemento | Convención | Ejemplo |
|---|---|---|
| Componentes | PascalCase + sufijo | `BoardComponent`, `TaskCardComponent` |
| Selectores | `app-` + kebab-case | `app-board`, `app-task-card` |
| Archivos | kebab-case + tipo | `board.component.ts`, `auth.service.ts` |
| Servicios | PascalCase + `Service` | `ProjectService`, `AuthService` |
| Interceptors/Guards | kebab-case + tipo | `auth.interceptor.ts`, `auth.guard.ts` |
| Carpetas de módulo (DDD ligero) | `core/`, `shared/`, `features/{feature}/` | `features/board/` |

### 7.3 Idioma

**Todo identificador de código en inglés** — clases, propiedades, tablas, columnas, variables, métodos, nombres de archivo (`User`, `Project`, `Column`, `Task`, `email`, `created_at`...). Decisión revisada durante el análisis de Fase 2 (2026-07-31): el enunciado usa terminología de dominio en español (Proyecto, Columna, Tarea) porque así describe el negocio, pero eso no obliga a que los identificadores del código la repliquen — el inglés es el estándar de facto en .NET/Angular y evita mezclar dos idiomas en la misma base de código a medida que crece. Detalle y alternativa descartada en `docs/decisions/arquitectura-decisiones.md` §12.

Lo que **sí** se mantiene en español, porque el evaluador y la sustentación son en español:
- Mensajes de validación/error que ve el usuario final (ej. `"El correo es obligatorio"`).
- Toda la documentación del proyecto: README, este documento, ADR, mensajes de commit.

La Fase 0-1 ya implementada (`Usuario`, `Correo`, tabla `usuarios`) se renombra en un retrofit dedicado para no dejar una inconsistencia permanente frente a Fase 2 en adelante, que ya nace en inglés.

---

## 8. Flujo de trabajo diario

Ciclo por tarea, heredado de `CLAUDE.md` § Flujo obligatorio antes de escribir código:

**Comprender → Analizar → Confirmar (si afecta arquitectura) → Implementar → Revisar → Commit**

Rutina sugerida por día de desarrollo (día 1 a 6; día 7 es buffer):

1. Revisar la issue/fase del día en el tablero (`En progreso`).
2. Aplicar el ciclo anterior por cada incremento pequeño de funcionalidad.
3. Commitear al cerrar cada incremento lógico, no al final del día — varios commits pequeños por sesión son la norma, no la excepción.
4. Actualizar `docs/decisions/arquitectura-decisiones.md` si surgió una decisión nueva no prevista.
5. Al cerrar la fase: abrir PR con el checklist de § 9, mergear, mover la issue a `Done`.

Definition of Ready (antes de mover una issue a `En progreso`): la issue referencia la sección del enunciado que cubre y tiene criterio de aceptación explícito.

---

## 9. Checklist de calidad

### 9.1 Definition of Done (por funcionalidad)

- [ ] Funciona end-to-end (backend + frontend si aplica).
- [ ] Tiene test donde el riesgo lo justifica (obligatorio si toca el algoritmo de ordenamiento).
- [ ] Arquitectura respetada (dominio no depende de infraestructura).
- [ ] Documentada si introdujo una decisión no trivial (`docs/decisions/`).
- [ ] Puedo explicarla en voz alta sin mirar el código — si no, no está lista.

### 9.2 Checklist de PR / autorrevisión

- [ ] Compila sin warnings nuevos.
- [ ] Tests existentes y nuevos pasan.
- [ ] Sin código muerto ni comentarios de debug (`Console.WriteLine`, `console.log` sueltos).
- [ ] Sin secretos ni connection strings hardcodeados.
- [ ] README actualizado si el cambio afecta cómo se ejecuta el proyecto.

### 9.3 Checklist de seguridad (OWASP-aligned, exigido en sección 4 del enunciado)

- [ ] Contraseñas con hash lento + salt/pepper (BCrypt/Argon2, nunca SHA256 puro).
- [ ] JWT valida `Issuer`, `Audience` y expiración.
- [ ] Todos los endpoints de negocio requieren autorización (nada abierto por descuido).
- [ ] Validación server-side con FluentValidation en todo input (nunca confiar solo en Angular).
- [ ] CORS con whitelist explícita, no `AllowAnyOrigin`.
- [ ] Rate limiting en login.
- [ ] Sin `[innerHTML]` sin sanitizar en Angular.
- [ ] Sin secretos versionados — todo via variables de entorno (`.env.example` documentado, `.env` real ignorado).

### 9.4 Checklist de entregables finales (sección 8 del enunciado)

- [ ] Repo público en GitHub con historial de commits distribuido en los 6 días.
- [ ] README completo: ejecución, decisiones, tiempo real elegido + alternativas, estrategia de ordenamiento, patrón de exportación, declaración de IA.
- [ ] Diagrama ERD como imagen embebida, generado desde las migraciones reales.
- [ ] `docker compose up` levanta todo en un entorno limpio siguiendo solo el README.
- [ ] Mínimo 5 tests backend + 5 frontend, incluyendo el del cálculo de posición.
- [ ] PDF del enunciado **no** está en el repositorio.
- [ ] Correo de entrega enviado a la dirección original del ejercicio con: URL del repo, README, ERD, video (si aplica) — antes de la fecha/hora límite (sección 9 del enunciado).
