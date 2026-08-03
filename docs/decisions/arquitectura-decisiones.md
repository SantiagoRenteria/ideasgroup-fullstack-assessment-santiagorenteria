# Decisiones de Arquitectura — Prueba Técnica IDEASGROUP-REM-LAT-26-2907

> Este documento consolida las decisiones arquitectónicas tomadas antes de iniciar el desarrollo, incluyendo alternativas evaluadas y descartadas, tal como exige la sección 8 del enunciado ("README con... decisiones arquitectónicas y su justificación").
>
> Documento vivo: se actualiza a medida que surgen nuevas decisiones durante el desarrollo. No se reescribe por encima — las decisiones superadas se marcan como tal, no se borran, para conservar la trazabilidad.

---

## 1. Stack Tecnológico

| Componente | Tecnología | Nota |
|---|---|---|
| Backend | .NET 8, C# | Versión exigida explícitamente en secciones 4 y 6.1 del enunciado |
| API | Minimal API | Endpoints agrupados por feature en métodos de extensión (`MapProyectoEndpoints(app)`), no controllers. Encaja con CQRS/MediatR ya elegido: cada endpoint reenvía al mediator sin una clase intermedia que solo delega. Decidido en Fase 0 (ver commit de cimientos del backend) |
| ORM | Entity Framework Core | Migraciones incrementales |
| Base de datos | PostgreSQL | — |
| Tiempo real | SignalR | Grupos por `boardId`/`proyectoId` |
| Reporte PDF | QuestPDF | Obligatorio por enunciado. Licencia Community (verificar umbral de ingresos) |
| Reporte Excel | ClosedXML | Elegido sobre EPPlus por licencia MIT sin restricciones comerciales |
| Validación backend | FluentValidation | Nunca confiar solo en validación de cliente |
| Validación frontend | Angular Reactive Forms | Reglas espejadas manualmente, no compartidas como librería |
| CQRS/Mediator | MediatR | Separación Commands/Queries, Pipeline Behaviors |
| Reverse proxy | Nginx | Ya exigido en 6.1 para servir el SPA; reutilizado como punto único de entrada (reemplaza necesidad de API Gateway dedicado) |
| Observabilidad | Serilog + OpenTelemetry SDK → Aspire Dashboard (standalone, contenedor único en docker-compose) | Ver sección 6 |
| Frontend | Angular 17, TypeScript, SCSS, PrimeNG (Sakai) | — |
| Contenedores | Docker Compose | PostgreSQL, API, SPA con Nginx, Aspire Dashboard |

### Alternativas evaluadas y descartadas

| Alternativa | Por qué se descartó |
|---|---|
| .NET 10 / C# 14 | El enunciado exige .NET 8 explícitamente (secciones 4 y 6.1). Se evaluó por ciclo de soporte más largo, pero desviarse de un stack especificado no es zona de criterio libre. |
| YARP API Gateway | No hay múltiples servicios que enrutar (arquitectura monolítica). Nginx, ya obligatorio para servir el SPA, cumple el mismo objetivo de punto único de entrada sin sumar un contenedor de riesgo adicional en el docker-compose del evaluador. |
| RabbitMQ | El requisito de tiempo real se resuelve completo con SignalR. RabbitMQ solo se justificaría para coordinar múltiples réplicas de la API, que este deployment no tiene. **Actualización (§24):** el Outbox Pattern sí se implementó (auditoría post-entrega) para la consistencia de la notificación del tablero — con polling + señal in-process, no un bus de mensajería; RabbitMQ se reevaluó en ese momento y se descartó por segunda vez, mismo motivo. |
| Prometheus + Grafana + Seq (stack completo) | Ningún punto del rubro de evaluación lo exige. Se optó por Aspire Dashboard standalone: un solo contenedor, mismos 3 pilares de observabilidad, menor riesgo de fallo en entorno del evaluador. |
| .NET Aspire AppHost (orquestación completa) | Reemplazaría el modelo de `docker compose up` exigido explícitamente en 6.1. Se usa solo el Aspire Dashboard como contenedor OTLP receptor, no el AppHost. |
| AutoMapper | Con 4 entidades de dominio, se prefiere mapeo manual centralizado en métodos de extensión (`ToDto()`) — más explícito y sin "magia" de reflection que defender en sustentación. |

---

## 2. Arquitectura

**Hexagonal (puertos y adaptadores)** en el backend, exigida explícitamente en secciones 4 y 6.1.

```
Domain/          → Entidades, Value Objects, reglas de negocio puras, sin dependencias externas
Application/     → Casos de uso (Commands/Queries MediatR), puertos (interfaces), DTOs, validadores
Infrastructure/  → Adaptadores: EF Core (repositorios), SignalR Hub, exportadores de reportes
API/             → Adaptador de entrada HTTP (endpoints), middlewares, configuración
```

- El **dominio no conoce EF Core** ni ningún framework externo.
- Los **puertos** (interfaces) se definen en `Application`; los **adaptadores** (implementaciones concretas) viven en `Infrastructure`.
- Justificación del Repository/Unit of Work sobre usar `DbContext` directo: en hexagonal, el repositorio es el puerto de persistencia — mantiene al dominio ignorante de EF Core, no es redundancia sin propósito.

Nota de transparencia: el equipo tiene experiencia previa en Clean/Onion Architecture, no en Hexagonal. Se optó por seguir el enunciado literal dado que la especificación es explícita y repetida dos veces en el documento, en vez de sustituir el paradigma por comodidad.

### 2.1 Arquitectura del Frontend

El enunciado (sección 4) exige explícitamente "separación por capas o hexagonal" también en el frontend, no solo en el backend. Se cumple con la organización DDD ligera ya definida en `docs/METODOLOGIA.md` §7.2:

```
core/              → servicios singleton, guards, interceptors, modelos transversales
shared/            → componentes/pipes/directivas reutilizables sin lógica de negocio propia
features/{feature}/ → módulos por dominio (board, auth, projects), con su propio service/state
```

Se descarta hexagonal literal (puertos/adaptadores) en Angular por sobre-ingeniería: el framework ya impone una separación por capas razonable (componentes ↔ servicios ↔ HTTP), y forzar el vocabulario de puertos/adaptadores en un SPA no aporta valor real frente a la opción explícitamente permitida ("por capas") en el propio enunciado.

---

## 3. Patrones de Diseño

| Patrón | Dónde se aplica | Justificación |
|---|---|---|
| CQRS | Application layer | Separa lectura de escritura; encaja naturalmente con hexagonal |
| Mediator (MediatR) | Application/API | Desacopla endpoints HTTP de lógica de negocio |
| Pipeline Behaviors | MediatR | Validación (FluentValidation) y logging transversal (`LoggingBehavior`, auditoría post-entrega §21) antes del Handler |
| Result Pattern | Domain/Application | Errores de negocio previsibles sin abusar de excepciones. `ErrorType` tipado (`NotFound`/`Conflict`/`Validation`/`Unauthorized`) mapea a HTTP status de forma centralizada (`ResultExtensions.ToErrorResponse()`) — reemplaza la convención original por contenido de mensaje, ver auditoría post-entrega §20 |
| Value Objects | Domain | `Email`, `DateRange`, `LexoRankKey` centralizan validación/normalización antes dispersa o ausente en las entidades — auditoría post-entrega §22 |
| Strategy + Factory | Módulo de reportes | Exportadores PDF/Excel intercambiables desde DTO común (ver sección 5) |
| Repository + Unit of Work | Infrastructure | Puerto de persistencia; dominio ignora EF Core |
| Outbox Pattern | Application/Infrastructure | Notificación del tablero por SignalR consistente con el commit de la mutación de negocio — auditoría post-entrega §24 |
| Options Pattern | Configuración | JWT, connection strings, inyectados tipados desde variables de entorno |

---

## 4. Estrategia de Ordenamiento — LexoRank simplificado

**Problema:** calcular la nueva posición de una tarea al reordenarla (única prueba unitaria nombrada explícitamente en el enunciado, sección 6.9).

**Alternativas evaluadas:**

| Opción | Descartada por |
|---|---|
| Índice entero secuencial (0,1,2,3...) | O(n) escrituras por movimiento; colisiona con edición concurrente de dos sesiones (escenario explícito en 6.7) |
| Posición fraccionada con `float` | Problema de precisión de punto flotante con reordenamientos frecuentes en el mismo hueco |
| Lista enlazada (`PreviousTaskId`) | Complica la lectura del tablero (requiere reconstrucción de cadena o CTE recursiva) a cambio de una inserción O(1) que no es el cuello de botella real aquí |

**Decisión: claves ordenables tipo string (LexoRank simplificado, base36/base62).**
- Insertar entre dos tareas genera una clave intermedia lexicográficamente ordenable.
- Cuando el espacio entre dos claves se agota, se genera una clave más larga (sin pérdida de precisión, a diferencia de floats).
- Rebalanceo periódico cuando el gap cae bajo un umbral definido.

**Cobertura mínima de la prueba unitaria obligatoria:**
1. Insertar entre dos posiciones existentes (caso normal).
2. Insertar al inicio y al final de la columna (bordes).
3. Caso límite que fuerza rebalanceo (gap agotado).

**Concurrencia:** control optimista con `xmin`/`RowVersion` de PostgreSQL a nivel de EF Core. Un conflicto de posición entre dos sesiones simultáneas (escenario de validación explícito en 6.7) debe ser detectado por el backend y responder error, disparando la reversión visible que exige 6.6. Sin esto, la reversión optimista nunca se activa en el flujo real.

---

## 5. Exportación Dual (PDF/Excel) — Diseño de Extensibilidad

Requisito 6.8: DTO común + una sola consulta, extensibilidad sin modificar clases exportadoras existentes.

```
Application/Reports/
├── ProjectReportDto.cs          (Proyecto + List<TareaReporteDto>)
├── GetProjectReportQuery.cs     (MediatR Query)
├── GetProjectReportHandler.cs   (una sola consulta EF, AsNoTracking, proyectada al DTO)
└── IReportExporter.cs           (puerto)

Infrastructure/Reports/
├── QuestPdfReportExporter.cs    (Format = "pdf")
└── ClosedXmlReportExporter.cs   (Format = "excel")
```

```csharp
public interface IReportExporter
{
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Export(ProjectReportDto dto);
}
```

El endpoint resuelve por inyección de `IEnumerable<IReportExporter>`, nunca por `if/switch` de formato — así, agregar un tercer formato es una clase nueva + un registro DI, sin tocar el endpoint ni las clases existentes. Content-Type explícito por formato (no `application/octet-stream` genérico). Nombre de archivo: `reporte-{proyecto}-{fecha}.{ext}`.

**Nota (2026-08-02):** el mecanismo concreto de "una sola consulta EF" y el nombre final de la query se refinaron durante la implementación de la Fase 5 — ver §18.

---

## 6. Observabilidad

- **Serilog**: logging estructurado en JSON. Hasta la Fase 6, esto solo capturaba dos cosas automáticas sin código de aplicación: `UseSerilogRequestLogging()` (una línea por request HTTP) y el logging interno de EF Core (SQL ejecutado) -- cero logging de aplicación, y la propiedad `UserId` mencionada más abajo no existía todavía (afirmación corregida en Fase 7, issue #37, ver el punto siguiente).
- **OpenTelemetry SDK** en la API, exportando trazas/métricas/logs vía OTLP hacia **Aspire Dashboard standalone** (contenedor único en docker-compose, sin AppHost). **No implementado** -- evaluado y descartado explícitamente por prioridad (ver Fase 7 más abajo), no es una omisión accidental.
- Sin puntos directos en el rubro de evaluación; se incluye como buena práctica de bajo riesgo de infraestructura, priorizado por debajo de todo lo obligatorio.

### 6.1 `ILogger` real en handlers críticos (Fase 7, issue #37)

Detectado al explicar en sesión por qué ninguna clase inyectaba `ILogger` (pregunta directa de Santiago): sin logging de aplicación, un error real en producción (login fallido, conflicto de concurrencia al mover una tarea) no dejaba ningún rastro diagnosticable más allá del código de estado HTTP.

- `ILogger<T>` inyectado en `LoginCommandHandler` y los 4 handlers de `Tasks` (Create/Update/Delete/Move) -- los puntos con mayor valor de diagnóstico, no todos los handlers del proyecto (criterio de riesgo, no cobertura numérica).
- `LogWarning` en cada ruta de fallo relevante: login con credenciales incorrectas (solo el correo, nunca la contraseña), entidad no encontrada, y el caso más valioso -- conflicto de concurrencia `ConcurrencyConflictException` al actualizar/eliminar/mover una tarea, exactamente el escenario que Santiago señaló como indiagnosticable.
- `LogContext.PushProperty("UserId", ...)` vía middleware en `Program.cs` (no en un handler): se ejecuta una vez por request, después de `UseAuthentication` y antes de `UseAuthorization` a propósito, para que incluso los intentos rechazados por autorización queden asociados a un usuario en los logs. Enriquece automáticamente *todo* log del resto del request (los de aplicación y los automáticos de EF/request-logging), sin tocar cada `LogWarning` individual.
- **Detalle no obvio verificado en runtime, no asumido:** el claim `UserId` se busca con `ClaimTypes.NameIdentifier`, no `JwtRegisteredClaimNames.Sub` -- `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` remapea "sub" a ese URI largo por defecto (junto con "email"→`ClaimTypes.Email`), pero **no** remapea "jti"/"name"/"exp" (por eso el logout y el indicador de presencia, Fase 4 y #24, ya funcionaban buscando esos claims por su nombre corto sin problema). Se comprobó volcando los claims reales del `ClaimsPrincipal` en runtime antes de asumir cuál era el nombre correcto -- una primera versión con `JwtRegisteredClaimNames.Sub` compilaba y corría sin error, pero producía `UserId: null` en todos los logs.

Verificado con `curl` contra la API real: login fallido queda logueado con el correo (`UserId: null`, correcto, todavía no autenticado); mover una tarea inexistente ya autenticado queda logueado con el `TaskId` **y** el `UserId` real del usuario.

---

## 7. Decisiones No Especificadas en el Enunciado

| Decisión | Resolución | Justificación |
|---|---|---|
| Tipo de "Responsable" en Tarea | FK nullable a Usuario | Habilita filtro por responsable (deseable 7) sin texto libre frágil; nullable permite backlog sin asignar |
| Valores de `Estado` de Proyecto | Planificado / EnProgreso / Completado / Cancelado | Enum fijo, sin máquina de transiciones (no exigido, evita scope creep) |
| Valores de `Prioridad` de Tarea | Baja / Media / Alta / Urgente | Enum fijo |
| Borrado de Proyecto con contenido | **Decisión superada, ver §13** — originalmente hard delete en cascada; revisado a soft-delete + regla "no borrar si tiene tareas" durante Fase 2 | Ver §13 para el detalle y la justificación del cambio |
| Dónde vive el cálculo de posición | Backend autoritativo (LexoRank); frontend reordena array localmente para optimistic update | Evita duplicar lógica de negocio crítica en dos lenguajes |
| Almacenamiento del JWT en cliente | **Decisión revisada, ver §17** — originalmente memoria pura; revisado a `sessionStorage` durante la Fase 4 | Ver §17 para el detalle y la justificación del cambio |
| Mapeo Entity↔DTO | Manual, centralizado en métodos de extensión por entidad | Explícito y defendible, sin duplicación inline en cada Handler |
| Diagrama de base de datos | Imagen PNG (ERD) embebida en README, generada desde el esquema real de las migraciones | Cumple la restricción explícita de 8: "imagen solamente, no textos de otras herramientas externas" |

---

## 8. Seguridad Adicional (no exigida explícitamente, pero cubierta por el criterio de "código seguro" de sección 2)

- CORS con whitelist explícita del origen del frontend (no `AllowAnyOrigin`).
- Rate limiting en el endpoint de login (`Microsoft.AspNetCore.RateLimiting` nativo de .NET 8).
- Validación explícita de `Issuer`, `Audience` y expiración en la configuración de `AddJwtBearer`.
- Sanitización de contenido de usuario en Angular (evitar `[innerHTML]` sin sanitizar en descripciones de tarea).
- Hash de contraseña con salt + pepper (algoritmo lento tipo BCrypt/Argon2, no SHA256 puro).

### 8.1 TLS/HTTPS — deliberadamente no implementado

Todo el stack corre en HTTP plano (`localhost:4200`, `localhost:5000`): no hay terminación TLS en nginx ni certificado en ningún punto. Es una decisión de alcance consciente, no una omisión:

- El enunciado (secciones 4 y 6.1) no exige HTTPS en ningún punto.
- La sección 12 aclara que el ejercicio es exclusivamente evaluativo, sin destino productivo — no hay tráfico real que proteger.
- Añadir un certificado autofirmado en nginx introduce un punto de fallo adicional en el arranque limpio que exige la sección 12 (`docker compose up` sin pasos manuales), y una advertencia de "certificado no confiable" en el navegador del evaluador que no aporta señal real sobre la calidad del código.

Esto **no es lo mismo** que "la contraseña viaja insegura por diseño": en un despliegue real, la protección en tránsito la da TLS (terminado en nginx, ya que es el punto único de entrada de la sección 1), nunca ofuscar o hashear la contraseña en el cliente antes de enviarla — eso convertiría el hash en la credencial real y sería *peor* que enviar la contraseña en texto plano sobre un canal cifrado (ataque tipo "pass-the-hash"). Si este proyecto pasara a producción, TLS en nginx sería el primer cambio de infraestructura, no un rediseño del flujo de login.

---

## 9. Otros Detalles No Exigidos pero Relevantes

- **Índices de base de datos**: `Tarea(ColumnaId, Orden)` compuesto para el reordenamiento; índice `pg_trgm` en `Proyecto.Nombre` para el filtro de coincidencia parcial (6.3) — un B-tree estándar no optimiza `ILIKE '%texto%'`.
- **Health checks**: `/health` para que `docker-compose` con `depends_on: condition: service_healthy` orqueste el arranque correctamente.
- **Seed data extra**: además de los 2 usuarios obligatorios, un proyecto de ejemplo con columnas y tareas precargadas, para que el evaluador pueda verificar tablero, drag&drop y tiempo real sin crear datos manualmente primero.

---

## 10. Plan de Fases (referencia)

Ver `docs/fases-implementacion.md` — resumen:

0. Cimientos (día 1) — estructura hexagonal, Angular+Sakai, docker-compose esqueleto
1. Autenticación (día 1-2) — JWT, hash, guardia de ruta, interceptor
2. CRUD Proyectos/Columnas (día 2-3)
3. Tablero + drag&drop + LexoRank (día 3-4) — TDD sobre el cálculo de posición primero
4. Tiempo real (día 4-5)
5. Reportes duales (día 5)
6. Pruebas restantes, README, diagrama ERD, opcionales (día 6)
7. Buffer, commits atómicos, video opcional (día 7)

---

## 11. Autenticación — Decisiones de Implementación (Fase 1)

### 11.1 BCrypt sobre Argon2

OWASP recomienda Argon2id como primera opción y BCrypt como segunda. Se eligió **BCrypt** (`BCrypt.Net-Next`) de todas formas: con un plazo de 7 días y sin necesidad de tunear memoria/paralelismo/iteraciones, la superficie de error de configurar mal Argon2 pesa más que la ganancia teórica de resistencia a GPU/ASIC en un sistema de 2 usuarios semilla sin registro público. Es una desviación consciente de la recomendación "ideal" de OWASP, justificada por gestión de riesgo bajo restricción de tiempo real — exactamente el tipo de trade-off que la sustentación técnica evalúa.

### 11.2 Mecanismo de pepper y su trade-off con la migración semilla

El pepper no se concatena directamente: se aplica `HMACSHA256(password, pepper)` y el resultado (no la contraseña original) es lo que entra a `BCrypt.HashPassword`. Esto evita el límite de 72 bytes de entrada de BCrypt y ata criptográficamente el pepper al hash sin que dependa de que BCrypt trunque la entrada silenciosamente.

**Trade-off importante:** el enunciado (6.2) exige "migración semilla" — EF Core `HasData` graba el hash en tiempo de diseño de la migración, no en tiempo de ejecución. Si `PASSWORD_PEPPER` (variable de entorno, mismo patrón que `JWT_SECRET`) cambia después de aplicar la migración, **los 2 usuarios semilla dejan de poder loguearse** porque el hash grabado ya no coincide con el pepper nuevo. Se acepta este trade-off — es consistente con cómo ya se comporta `POSTGRES_PASSWORD` frente a un volumen de datos ya creado — y se documenta explícitamente en el README: no cambiar `PASSWORD_PEPPER` sin regenerar la migración `InitialCreate`.

### 11.3 JWT

Claims: `sub` (Id de usuario), `email`, `name`, `jti` (identificador único del token). `TokenValidationParameters` valida `Issuer`, `Audience`, expiración (`ClockSkew = TimeSpan.Zero`, sin margen de tolerancia) y la firma con la misma clave simétrica usada para firmar. Configuración vía Options Pattern (`JwtOptions`, `SecurityOptions`) enlazada a `Jwt:*` y `Security:Pepper`, alimentada por variables de entorno en `docker-compose.yml` — nunca hardcodeada.

### 11.4 Errores de negocio (Result) vs errores de formato (excepción + middleware)

`LoginCommandHandler` devuelve `Result<LoginResponseDto>.Failure(...)` para credenciales inválidas — mapeado explícitamente a 401 en el endpoint. El mensaje es genérico ("Correo o contraseña incorrectos") para no revelar si el correo existe (evita enumeración de usuarios). En cambio, `FluentValidation` (formato de correo vacío/inválido) lanza `ValidationException`, capturada por un middleware global (`app.UseExceptionHandler`) que la traduce a 400. La distinción es deliberada: el Result Pattern es para reglas de negocio previsibles por handler; los errores de formato de entrada son estructurales y se resuelven una sola vez, de forma centralizada.

### 11.5 ICommand/IQuery explícitos

MediatR no distingue Command de Query a nivel de tipos — ambos son `IRequest<TResponse>`. Para que la separación de CQRS declarada en la sección 3 sea verificable por el compilador y no solo por convención de nombres, se agregaron `ICommand<TResponse>` e `IQuery<TResponse>` (ambas heredan de `IRequest<TResponse>`) en `Application/Common/Messaging`. `LoginCommand` implementa `ICommand<Result<LoginResponseDto>>`.

Nota de transparencia: `LoginCommand` no escribe nada en la base de datos actualmente (no actualiza último login ni crea un registro de sesión), así que un purista de CQRS lo modelaria como Query. Se mantiene como Command porque representa una accion de seguridad auditable y porque es previsible que en Fase 7 (bloqueo de cuenta / rate limiting) empiece a tener efectos de escritura — cambiar su forma en ese momento seria mas costoso que anticiparla ahora. Es una decision discutible, documentada aqui a proposito para poder defenderla o revisarla en la sustentacion.

### 11.6 Minimal API de autenticación

`POST /api/auth/login` vive en `Endpoints/AuthEndpoints.cs` (`MapAuthEndpoints`), consistente con la decisión de la sección 3. El DTO de request (`LoginRequest`) es propio de la capa API — no se reutiliza `LoginCommand` de Application directamente en el contrato HTTP, para no acoplar el shape de la API a la forma interna del caso de uso.

---

## 12. Idioma del código — revisión de la decisión inicial (Fase 2)

**Decisión superada** (no se borra, se documenta el cambio — regla de este archivo): `docs/METODOLOGIA.md` §7.3 establecía originalmente "dominio y nombres de negocio en español (Proyecto, Columna, Tarea — como en el enunciado); nombres técnicos genéricos en inglés". Bajo esa regla, Fase 1 se implementó con `Usuario`, `Correo`, `Nombre`, tabla `usuarios`.

**Decisión nueva (2026-07-31):** todo identificador de código pasa a inglés (`User`, `Project`, `Column`, `Task`, `Email`, `Name`...). Los mensajes de validación/error orientados al usuario y toda la documentación del proyecto se mantienen en español.

**Por qué se revierte:**
- El enunciado usa "Proyecto/Columna/Tarea" para describir el dominio de negocio en la sección que lo redacta, no como una exigencia de que los identificadores de código repliquen ese idioma — es una lectura, no un requisito literal como sí lo es el stack tecnológico (sección 4).
- Mezclar idiomas dentro del mismo código (`Usuario` en Fase 1 conviviendo con `Project`/`Column` en Fase 2 si no se corrige) es más difícil de defender en la sustentación que una convención uniforme — un evaluador senior lo marca como inconsistencia de criterio, no como decisión de diseño.
- Inglés en identificadores es el estándar de facto en .NET/Angular; reduce fricción si el código se reutiliza o revisa fuera de un contexto hispanohablante.

**Alternativa descartada:** mantener el dominio en español solo por fidelidad literal al enunciado, aceptando la mezcla de idiomas. Se descarta porque el enunciado (sección 9) exige justificar decisiones, y "porque el PDF usa esas palabras" no es una justificación técnica defendible frente a la inconsistencia que genera.

**Retrofit de Fase 0-1:** `Usuario` → `User`, `Correo` → `Email`, `Nombre` → `Name`, tabla `usuarios` → `users`, columnas `correo`/`nombre` → `email`/`name`, `LoginCommand(string Correo, string Password)` → `LoginCommand(string Email, string Password)`. Requiere regenerar la migración `InitialCreate` (aún no hay datos reales en ningún entorno del evaluador, así que no hay migración de datos que preservar) y actualizar el frontend (`UsuarioSesion`, campo `correo` en los modelos de auth). Se ejecuta como rama `fix/rename-domain-to-english` con PR propio, separada de `feature/projects-columns` (Fase 2), para no mezclar un renombrado mecánico con funcionalidad nueva en el mismo commit.

---

## 13. Borrado de Proyecto — revisión de la decisión inicial (Fase 2)

**Decisión superada** (no se borra, se documenta el cambio — regla de este archivo): la sección 7 originalmente establecía hard delete en cascada (Tareas → Columnas → Proyecto) para el borrado de un Proyecto con contenido, vía `ON DELETE CASCADE` a nivel de FK, justificado porque "soft-delete añade filtros globales de query sin estar exigido".

**Decisión nueva (2026-08-01):** dos cambios, decididos juntos durante la implementación del CRUD de Fase 2:

1. **Regla de negocio nueva:** no se permite eliminar un Proyecto que contenga tareas (en cualquiera de sus columnas) — mismo criterio ya exigido por el enunciado (sección 6.4) para Columna, extendido a Proyecto. `DeleteProjectCommandHandler` devuelve 409 si `IColumnRepository.ProjectHasTasksAsync` es verdadero.
2. **Soft-delete en Project, Column y TaskEntity** (no en User — no existe ninguna operación de borrado de usuario en el proyecto, así que agregar `IsDeleted` ahí sería scope creep sin caso de uso real). Cada entidad tiene `IsDeleted`/`DeletedAt` y un método de dominio `Delete()`. `HasQueryFilter(e => !e.IsDeleted)` en cada `IEntityTypeConfiguration` excluye automáticamente las filas borradas de toda consulta LINQ, sin repetir el filtro en cada Handler.

**Por qué se revierte:**
- El trade-off original (filtros globales de query) sigue siendo real, pero se acepta ahora como costo consciente: mantener un historial de qué se borró y cuándo (auditoría) pesa más que la complejidad que añade, una vez que ya existe precedente de manejar una regla de bloqueo similar en Columna.
- Con la regla de negocio del punto 1 ya vigente, `DeleteProjectCommandHandler` nunca cascada sobre Tareas reales (si las hay, el borrado se bloquea antes) — el soft-delete en cascada solo alcanza a Columnas vacías, lo que simplifica la implementación real frente al caso general.

**Implementación:**
- FK `Column → Project` y `TaskEntity → Column` cambian de `Cascade` a `Restrict`: con soft-delete, la app nunca debe emitir un `DELETE` físico sobre estas tablas: un `Restrict` actúa como red de seguridad a nivel de base de datos ante ese caso, en vez de propagarlo silenciosamente si ocurriera por error o por acceso directo a la BD.
- El borrado de un Proyecto marca el proyecto (`SaveChanges`) y sus columnas (`ExecuteUpdateAsync` en bloque, sin cargar cada entidad a memoria) dentro de una transacción explícita (`IUnitOfWork.ExecuteInTransactionAsync`) — son dos escrituras separadas que antes eran una sola sentencia `DELETE ... CASCADE`, y se preserva la misma garantía de atomicidad.
- Índices únicos (ej. `ix_users_email`) no se ven afectados porque User queda fuera del alcance de este cambio.

**Alternativa descartada:** definir "tareas en curso" (en vez de "cualquier tarea") como condición de bloqueo del borrado de Proyecto. Se descarta por ahora porque `TaskEntity` no tiene un campo de estado — en un Kanban, el estado de una tarea *es* la columna en la que vive, y `Column` todavía no distingue cuál columna representa "completado" (no hay flag `IsTerminal` ni equivalente). Añadir esa distinción es una decisión de modelo que pertenece a Fase 3, cuando se termina de definir el comportamiento de `Task`/`Column` en profundidad — hacerlo ahora habría sido adelantar diseño sin la información completa del tablero.

**Nota de transparencia — deuda parcialmente resuelta:** la sección 6 documentaba Serilog + OpenTelemetry (Aspire Dashboard) como decisión de observabilidad desde Fase 0, pero nunca se había implementado en código. El 2026-08-01 se implementó la mitad de bajo riesgo: **Serilog** con formato JSON compacto (`CompactJsonFormatter`) a consola, `UseSerilogRequestLogging()` para log estructurado por request, logger de bootstrap para capturar errores de arranque, y niveles configurables vía `appsettings.{Environment}.json` (`Serilog:MinimumLevel`, reemplazando la sección `Logging` por defecto). Sin dependencias nuevas en `docker-compose.yml` — sigue escribiendo a stdout del contenedor `api`, ya capturado por `docker compose logs`.

**Sigue pendiente**: OpenTelemetry SDK + contenedor Aspire Dashboard como receptor OTLP. Se difiere a propósito porque agrega un servicio nuevo a `docker-compose.yml` (más superficie de fallo en el arranque limpio que exige la sección 12 del enunciado) y no aporta puntos directos en el rubro de evaluación (sección 10) frente al resto del backlog obligatorio (Fases 3-5) que sí los aporta. Se evalúa retomarlo en Fase 6 si el tiempo lo permite.

---

## 14. Diseño de Tareas y Tablero — decisiones previas a la implementación (Fase 3)

Antes de escribir código de Fase 3 (CRUD de Tareas, tablero kanban, drag&drop, LexoRank), se resolvieron cuatro decisiones de diseño no cubiertas en detalle por las secciones anteriores. Se documentan aquí, confirmadas explícitamente antes de implementar (regla del flujo obligatorio de `CLAUDE.md`).

### 14.1 `MoveTaskCommand` separado de `UpdateTaskCommand`

**Decisión:** dos Commands distintos — `UpdateTaskCommand` (título, descripción, prioridad, responsable) y `MoveTaskCommand` (columna destino, nueva posición) — en vez de un único comando de actualización genérico que acepte todos los campos incluidos `ColumnId`/`Order`.

**Por qué:** el propio enunciado (6.7) distingue "alta, edición y eliminación de tareas" de "traslado y nuevo orden" como dos categorías de evento separadas para la propagación en tiempo real. Modelar esa distinción ahora en el backend (Fase 3) evita un refactor en Fase 4, cuando cada Command necesitará disparar un evento SignalR distinto (`TaskUpdated` vs `TaskMoved`). También mantiene cada Handler con una responsabilidad más estrecha: `MoveTaskCommandHandler` solo recalcula posición/columna, sin validar los demás campos de negocio.

**Alternativa descartada:** un `UpdateTaskCommand` único que acepte todos los campos como opcionales. Se descarta porque mezclaría dos intenciones de negocio distintas en un mismo Handler y complicaría la futura emisión de eventos diferenciados.

### 14.2 Concurrencia optimista (`RowVersion`/`xmin`) diferida a Fase 4

**Decisión:** `TaskEntity` no incorpora `RowVersion` en Fase 3. La reversión visible que exige 6.6 ("actualización optimista con reversión si el servidor responde con error") se cubre en esta fase con cualquier error HTTP normal (404 si la tarea fue borrada, 400 si la columna destino no es válida) — no requiere detección de conflicto de concurrencia real.

**Por qué:** el escenario que de verdad ejercita la concurrencia optimista (dos sesiones moviendo la misma tarea al mismo tiempo, sección 6.7) depende de tener el canal de tiempo real operativo, que es Fase 4. Implementar `RowVersion` ahora exigiría una migración y lógica de detección de conflicto que no se puede probar de forma realista sin dos sesiones concurrentes — sería complejidad adelantada sin poder validarla. `docs/decisions/arquitectura-decisiones.md` §4 sigue vigente como diseño objetivo; esta entrada documenta cuándo se materializa.

**Alternativa descartada:** implementar `RowVersion` ya en Fase 3 tal como lo describe el §4 original. Se descarta por ahora — no por estar mal, sino porque el momento correcto de implementarlo es cuando exista el escenario real que lo dispara (Fase 4), evitando código sin cobertura de prueba significativa en el ínterin.

### 14.3 Navegación `Column.Tasks`

**Decisión:** se agrega `IReadOnlyCollection<TaskEntity> Tasks` a `Column`, respaldada por una lista privada `_tasks`, siguiendo el mismo patrón ya usado en `Project.Columns`.

**Por qué:** consistencia con el patrón existente (`Project` ya expone `Columns` de la misma forma) y necesidad real: el endpoint agregado de tablero (§14.4) necesita cargar columnas con sus tareas en una sola consulta (`Include`), y la navegación EF Core es la forma idiomática de expresar esa relación sin duplicarla como query suelta en cada Handler que la necesite.

**Alternativa descartada:** resolver el tablero completo por consulta explícita combinando `IColumnRepository` + `ITaskRepository` sin navegación en la entidad. Se descarta porque Column ya tiene precedente de exponer su colección relacionada (Project↔Column) y romper esa consistencia solo para Task no tiene justificación adicional.

### 14.4 Endpoint agregado `GET /api/projects/{id}/board`

**Decisión:** se agrega un endpoint específico que devuelve, en una sola respuesta, las columnas del proyecto con sus tareas ya anidadas y ordenadas — en vez de que el frontend componga el tablero llamando a `GET /api/columns?projectId=` seguido de N llamadas `GET /api/tasks?columnId=` (una por columna).

**Por qué:** el tablero es la vista principal de Fase 3 y se carga completa en cada visita/recarga (requisito 6.6: persistencia verificable al recargar). Una sola consulta EF Core proyectada (columnas + tareas vía `Include`, ordenadas por `Column.Order` y `TaskEntity.Order`) evita tanto el problema de N+1 peticiones HTTP como el riesgo de N+1 queries si se arma por columna. Los endpoints CRUD individuales de tareas (`POST/PUT/DELETE /api/tasks`) siguen existiendo para las mutaciones puntuales del tablero; el endpoint de `board` es de solo lectura, para la carga inicial y la recarga completa.

**Alternativa descartada:** componer el tablero en el frontend combinando los endpoints ya existentes de columnas y tareas. Se descarta por el costo de N round-trips en tableros con varias columnas y porque duplicaría en el cliente una lógica de ensamblado que pertenece al servidor.

---

## 15. Diseño de Tiempo Real — decisiones previas a la implementación (Fase 4)

Antes de escribir código de Fase 4 (canal en tiempo real, sección 6.7 del enunciado), se resolvieron cuatro decisiones de diseño, confirmadas explícitamente antes de implementar (regla del flujo obligatorio de `CLAUDE.md`). La tecnología (SignalR) y el criterio de agrupación (grupos por `boardId`/`proyectoId`) ya estaban decididos desde la Fase 0 (§1, §2); esta sección cubre lo que faltaba definir.

### 15.1 Ubicación del Hub y del notificador: Infrastructure, no API

**Decisión:** `IBoardNotifier` es un puerto en `Application` (una interfaz con métodos `NotifyTaskCreatedAsync`, `NotifyTaskUpdatedAsync`, `NotifyTaskDeletedAsync`, `NotifyTaskMovedAsync`). El adaptador concreto (`SignalRBoardNotifier`, usando `IHubContext<BoardHub>`) y el propio `BoardHub` viven en `Infrastructure`, que agrega el paquete `Microsoft.AspNetCore.SignalR.Core` (no requiere el SDK Web completo, solo esa librería). `API` se limita a mapear la ruta del hub (`app.MapHub<BoardHub>("/hubs/board")`) y a la configuración de autenticación del canal.

**Por qué:** mantiene la simetría ya establecida — `Infrastructure` es donde viven **todos** los adaptadores hacia tecnología externa (EF Core, BCrypt/JWT, y ahora SignalR), nunca en `API`. Los Command Handlers de `Application/Tasks` dependen únicamente del puerto `IBoardNotifier`, igual que dependen de `ITaskRepository` — no conocen SignalR, lo que permite reemplazar la tecnología de tiempo real sin tocar un solo Handler (mismo argumento que ya se usó para Repository/Unit of Work en §2).

**Alternativa descartada:** Hub e `IHubContext` en `API`, aprovechando que ese proyecto ya referencia el framework ASP.NET Core completo sin fricción de paquetes. Se descarta porque rompería la única regla arquitectónica no negociable del enunciado (hexagonal, secciones 4 y 6.1): `API` pasaría a contener lógica de adaptador de infraestructura, no solo enrutamiento HTTP/WebSocket de entrada.

### 15.2 Concurrencia optimista con `xmin` — se materializa ahora

**Decisión:** se implementa lo que el §14.2 dejó pendiente: `TaskEntity` mapea la columna de sistema `xmin` de PostgreSQL como token de concurrencia (`builder.UseXminAsConcurrencyToken()` en `TaskEntityConfiguration`). `UpdateTaskCommandHandler`, `MoveTaskCommandHandler` y `DeleteTaskCommandHandler` capturan la excepción de concurrencia (traducida por `UnitOfWork` a `ConcurrencyConflictException`, propia de `Application`, para no filtrar `DbUpdateConcurrencyException` de EF Core hacia arriba) y devuelven `Result.Failure` (409), que ya dispara la reversión visible de 6.6 sin código adicional en el frontend.

**Corrección sobre el supuesto inicial (transparencia):** se asumió que mapear `xmin` no requeriría una migración nueva, por ser una columna de sistema ya presente en toda tabla de Postgres. Al generar la migración de verificación, EF Core sí detectó el nuevo shadow property y generó un `AddColumn` — que además **fallaría en runtime**, porque `xmin` es un nombre de columna reservado por Postgres (`column name "xmin" conflicts with a system column name`). La migración (`AddXminConcurrencyTokenToTasks`) se conserva pero con `Up`/`Down` vacíos a propósito: solo deja constancia en el historial de EF de que `TaskEntity` empezó a usar `xmin`, sin ejecutar ningún DDL real. Se documenta el error de estimación en vez de corregir el ADR en silencio.

**Por qué (a pesar de la migración adicional):** el enunciado 6.7 exige literalmente *propagar* cambios, no detectar conflictos de escritura simultánea sobre la misma tarea — así que esto no es un requisito obligatorio evaluado por nombre. Pero el costo real de implementarlo vía `xmin` sigue siendo bajo (una migración vacía + unas pocas líneas en tres Handlers, sin columna nueva de verdad) frente al valor de cerrar un caso real que Fase 4 introduce por primera vez: dos sesiones pueden ahora editar la misma tarea al mismo tiempo de verdad (antes de tener tiempo real, ese escenario era teórico). Se prioriza cumplir la promesa que el propio ADR dejó escrita en §14.2 en vez de volver a diferirla sin una razón nueva.

**Alternativa descartada:** no implementarlo y revisar el ADR §14.2 documentando que 6.7 no lo exige literalmente. Se descarta porque el costo de implementarlo (bajo, incluso con la migración vacía) es menor que el costo de defender en la entrevista por qué se prometió dos veces (§4 y §14.2) y no se hizo ninguna de las dos.

**Alcance:** solo `TaskEntity` (donde ahora hay edición concurrente real vía tiempo real). No se agrega a `Column` — el reordenamiento de columnas sigue siendo de baja frecuencia y de un solo usuario administrador a la vez, sin el escenario de dos sesiones simultáneas que sí existe en el tablero activo.

### 15.3 El emisor de un cambio no recibe su propio evento por WebSocket

**Decisión:** todas las notificaciones usan `Clients.OthersInGroup(groupName, connectionId)`, nunca `Clients.Group(...)`. El `connectionId` del emisor se resuelve en el endpoint HTTP (via un header o el propio `HubConnection.connectionId` enviado desde el frontend, ver detalle de implementación) y se propaga hasta el notificador.

**Por qué:** el emisor ya actualizó su UI de forma optimista con la respuesta HTTP (mecanismo de 6.6, ya implementado en Fase 3). Si además recibiera su propio evento por el socket, el frontend tendría que distinguir "esto ya lo apliqué localmente" de "esto es nuevo" para no aplicar `moveItemInArray`/`transferArrayItem` dos veces sobre el mismo movimiento — complejidad de deduplicación que no aporta nada, ya que el resultado final es idéntico.

**Alternativa descartada:** `Clients.Group(...)` (incluir al emisor) con deduplicación en el frontend por `taskId` + timestamp o comparando el estado ya aplicado. Se descarta por complejidad innecesaria — excluir al emisor en el backend es una línea de código; deduplicar en el cliente es lógica adicional que además es más frágil (depende de comparar estado, no de una propiedad estructural del mensaje).

### 15.4 Conexión SignalR con alcance de componente (no un servicio de sesión compartido)

**Decisión:** `BoardComponent` crea la conexión (`HubConnectionBuilder`) y se une al grupo del tablero en `ngOnInit`, y la cierra (`hubConnection.stop()`) en `ngOnDestroy`. No existe un servicio raíz que mantenga la conexión viva entre navegaciones.

**Por qué:** el enunciado (6.7, último punto) pide explícitamente "cierre correcto de la conexión y de las suscripciones **al destruir el componente**, sin conexiones huérfanas" — atar el ciclo de vida de la conexión al ciclo de vida del único componente que la usa (`BoardComponent`) hace que ese requisito sea trivialmente verificable (no hay ambigüedad sobre cuándo debe cerrarse) y evita mantener un socket abierto cuando el usuario ni siquiera está viendo un tablero.

**Alternativa descartada:** un servicio singleton (`RealtimeService` a nivel de `core/`) que mantiene la conexión durante toda la sesión autenticada. Se descarta por sobre-ingeniería para el alcance actual — solo existe una vista que consume tiempo real (`BoardComponent`); un servicio compartido solo se justificaría si varias vistas necesitaran la misma conexión simultáneamente, lo que no ocurre en este proyecto.

### 15.5 Eventos emitidos y su forma

**Decisión:** cuatro eventos, uno por operación de negocio ya separada en Application (§14.1): `TaskCreated`, `TaskUpdated`, `TaskDeleted`, `TaskMoved`. Los tres primeros llevan el `TaskResponseDto` ya existente (mismo shape que la API REST); `TaskDeleted` lleva solo `{ taskId, columnId }` (no hay DTO de una entidad borrada); `TaskMoved` lleva `{ taskId, targetColumnId, targetIndex, order }` — el índice que el propio emisor ya usó para calcular la posición, para que las demás sesiones apliquen el mismo `moveItemInArray`/`transferArrayItem` que ya usa `BoardComponent.onDrop` en vez de recalcular posiciones a partir del string de orden.

**Por qué:** reutilizar los DTOs y el vocabulario de eventos que ya distingue Update de Move (§14.1) evita introducir un segundo modelo de datos solo para tiempo real. Enviar `targetIndex` (no solo `order`) permite que el frontend aplique el mismo código de reordenamiento de array que ya tiene y ya está probado (`board.component.spec.ts`), en vez de escribir una segunda función que reconstruya el índice a partir de la clave LexoRank.

---

## 16. Cierre de sesión con revocación real de JWT

No exigido por el enunciado, pedido explícitamente durante la Fase 4 (mostrar el usuario logueado en el nav y un logout que invalide el token). Se presentaron dos alcances antes de implementar: (a) limpiar el token solo en el cliente (lo que `AuthService.logout()` ya hacía) o (b) revocación real en servidor. Se confirmó (b).

**Decisión:** blocklist de JWT revocados por `jti`. `POST /api/auth/logout` (autenticado) lee el `jti` y el `exp` del propio token de la petición y los persiste; `JwtBearerEvents.OnTokenValidated` (Infrastructure) rechaza cualquier request cuyo `jti` esté en la blocklist, en cada endpoint protegido **y** en el hub de SignalR (comparten la misma configuración de `AddJwtBearer`, ver §15).

**Por qué un `jti` no es un token completo:** revocar por `jti` (un identificador corto, no el JWT completo) evita que la blocklist crezca con strings largos y evita tener que parsear/comparar el token entero en cada request — el middleware de validación ya expone los claims decodificados, `jti` incluido.

**Dónde vive la entidad de revocación:** `RevokedToken` (Infrastructure/Persistence/Entities, **no** `Domain/Entities`) — es un registro técnico de seguridad sin reglas de negocio propias (no tiene invariantes, no participa de ningún caso de uso de negocio), a diferencia de `User`/`Project`/`Column`/`TaskEntity`. Coherente con cómo `IPasswordHasher`/`IJwtTokenGenerator` ya se tratan como infraestructura de seguridad y no como dominio.

**Alternativa descartada — limpieza periódica de tokens expirados:** la tabla `revoked_tokens` no tiene un job de limpieza en background. Se acepta el crecimiento no acotado (bajo, con 2 usuarios semilla y tokens de corta duración) en vez de agregar un `BackgroundService` adicional; `ExpiresAtUtc` se persiste igual, dejando la puerta abierta a un `DELETE WHERE expires_at < now()` si el volumen real lo justificara.

**Alternativa descartada — refresh tokens:** resolvería la revocación de forma más "estándar" (access token de vida muy corta + refresh token de vida larga, revocable), pero es una reestructuración completa del flujo de autenticación (nuevo endpoint, nuevo almacenamiento, rotación) no pedida y fuera de alcance para un cierre de sesión explícito por parte del usuario.

**Frontend:** `AuthService.logout()` llama a `POST /api/auth/logout` (si hay token) y limpia el estado local en memoria y navega al login tanto si la llamada tiene éxito como si falla — un fallo de red en el logout no debe dejar al usuario atrapado en una sesión que ya quiere cerrar. `AuthInterceptor` excluye `/auth/logout` (además de `/auth/login`, ya excluido) del logout automático por 401, para no disparar una segunda llamada de logout recursiva si el propio logout devuelve 401.

---

## 17. Almacenamiento del JWT en cliente — revisión de la decisión inicial (Fase 4)

**Decisión superada** (no se borra, se documenta el cambio — regla de este archivo): §7 establecía JWT solo en memoria (variable de instancia de `AuthService`), explícitamente **sin** `localStorage`. La justificación original solo comparaba memoria/`localStorage` contra cookie httpOnly (el enunciado 6.2 exige un interceptor que adjunte el token, lo que descarta la cookie automática) — nunca argumentó memoria por sobre `sessionStorage` específicamente.

**Cómo se detectó:** al usar la aplicación de forma más activa durante la Fase 4 (agregar el nombre de usuario y el logout al nav), recargar la página con una sesión activa siempre desloguea — la memoria de la SPA se destruye por completo en cada F5. Es el costo real, no solo teórico, de "memoria pura", y no estaba señalado como una fricción de UX esperada en ningún lado del README ni del ADR.

**Decisión nueva (2026-08-01):** el JWT y los datos del usuario actual (`{name, email}`) se guardan en `sessionStorage` en vez de en una variable de instancia.

**Por qué:**
- `sessionStorage` sigue exigiendo que `AuthInterceptor` adjunte el header manualmente (no es automático como una cookie) — la razón original del ADR para descartar la cookie httpOnly sigue vigente y **no** aplica como argumento en contra de `sessionStorage`.
- Sobrevive a recargar la página (soluciona la fricción real detectada) pero se pierde al cerrar la pestaña o el navegador — no es tan persistente como `localStorage`.
- Sigue siendo accesible desde JavaScript (como memoria o `localStorage`), así que la superficie de riesgo ante un XSS no cambia en calidad — solo en duración: con memoria pura, un token robado deja de servir en cuanto la pestaña se recarga o cierra; con `sessionStorage`, sirve mientras la pestaña siga abierta (pero no sobrevive a cerrarla, a diferencia de `localStorage`).

**Alternativa descartada — `localStorage`:** sobrevive incluso a cerrar y reabrir el navegador, la opción más cómoda para el usuario. Se descarta porque un token robado por XSS seguiría sirviendo indefinidamente (hasta su expiración natural) sin importar si la víctima cierra el navegador — una ventana de exposición mayor que no se justifica solo por comodidad, dado que igual hace falta el interceptor en ambos casos.

**Alternativa descartada — mantener memoria pura:** es la opción más resistente a XSS (nada persiste nunca, ni siquiera dentro de la misma pestaña tras un F5), pero la fricción de UX (perder la sesión en cada recarga accidental) pesa más que esa ganancia marginal de seguridad para una aplicación de evaluación con 2 usuarios semilla — el propio enunciado no exige ningún nivel de persistencia de sesión, así que es una decisión de criterio, no de cumplimiento.

---

## 18. Exportación Dual — refinamiento del diseño al implementar (Fase 5, issue #16)

§5 dejó el mecanismo de "una sola consulta EF" descrito a nivel de intención, sin especificar cómo encajaba con el patrón de puertos ya establecido en el resto del proyecto (`IProjectRepository`, `IColumnRepository`, `ITaskRepository`, todos con métodos que devuelven entidades de dominio, ninguno con un join agregado). Al implementar, esto exigió dos decisiones concretas que no estaban en §5:

**1. Puerto de solo lectura dedicado — `IProjectReportRepository`.** El precedente más cercano (`GetProjectBoardQueryHandler`, Fase 3) dispara tres consultas separadas (proyecto, columnas, tareas) y las agrupa en memoria — no cumple el criterio literal de la issue #16 ("una sola consulta EF"). Se descartó extender `ITaskRepository` con un método de reporte (le agregaría una tercera responsabilidad a una interfaz que ya distingue explícitamente tracking-para-reordenar de solo-lectura-para-tablero) y se descartó inyectar `AppDbContext` directo en el handler (rompería el único invariante que sí se respeta en todo `Application/` hasta ahora: ningún handler referencia EF directamente). `IProjectReportRepository.GetReportAsync(projectId, ct)` vive junto a los demás repositorios en `Common/Interfaces/`, con un solo método.

**2. La consulta arranca desde `Projects`, no desde `Tasks`.** La primera versión evaluada partía de `Tasks` con `join` hacia `Columns`/`Projects`/`Users` — pero un proyecto con cero tareas (columnas vacías, o incluso cero columnas) no produce ninguna fila, y eso es indistinguible de "el proyecto no existe" (ambos casos deben responder distinto: 404 vs. reporte vacío). La consulta final es un `LEFT JOIN` encadenado `Project -> Columns -> Tasks -> User`, con `DefaultIfEmpty()` en cada salto: garantiza al menos una fila por proyecto existente (con los campos de tarea en `null` si no hay tareas), y cero filas solo si el proyecto no existe. Verificado contra Postgres real (no solo compilación): EF Core 8 traduce el chain completo a un único `SELECT ... FROM projects LEFT JOIN (...) LEFT JOIN (...) LEFT JOIN users ...` que respeta los `HasQueryFilter` de soft-delete sin repetirlos a mano.

**3. `ExportProjectReportQuery` (no `GetProjectReportQuery`) es el único query — sin separar "obtener DTO" de "exportar".** Aunque "exportar" parece una acción, no muta estado persistente (no hay `SaveChangesAsync`), así que cae del lado Query de CQRS por definición — la frontera es "¿muta estado?", no "¿hace trabajo?". El handler es el único orquestador: pide el DTO al repositorio, resuelve el `IReportExporter` por `Format` (`OrdinalIgnoreCase`, sin `if/switch`), y estampa `GeneratedAt = DateTime.UtcNow` (no es dato persistido, no le corresponde al repositorio). No se creó un `GetProjectReportQuery` previo y separado porque nada en las issues #16-#19 lo necesita — habría sido código sin consumidor (YAGNI). Los `IReportExporter` (`QuestPdfReportExporter`, `ClosedXmlReportExporter`) no pasan por el pipeline de MediatR: son transformaciones puras `DTO -> bytes`, resueltas por DI directo en el handler, no comandos ni queries por sí mismos.

---

## 19. Endurecimiento de seguridad (Fase 7, issue #26)

Auditoría contra el checklist de `docs/METODOLOGIA.md` §9.3 encontró 2 de 4 puntos ya cumplidos (FluentValidation en todo input de usuario, sin secretos versionados) y 2 gaps reales, cerrados en esta fase:

**CORS con whitelist explícita.** `CORS_ALLOWED_ORIGIN`/`Cors__AllowedOrigin` ya existían en `.env.example`/`docker-compose.yml` desde el diseño inicial, pero nunca se consumían — ningún `AddCors`/`UseCors` en todo el backend (detectado durante la Fase 5, corregido recién ahora). `CorsOptions` (Infrastructure/Security) se bindea a la sección `Cors` y `AddDefaultPolicy` solo habilita el origen configurado, sin `AllowAnyOrigin`. Sin `AllowCredentials`: la API usa JWT por header `Authorization`, no cookies, así que no hace falta y además es incompatible con especificar orígenes concretos en la misma política. Verificado con `curl -X OPTIONS` simulando un preflight: `http://localhost:4200` recibe `Access-Control-Allow-Origin` en la respuesta, `http://evil.com` no lo recibe (el navegador bloquearía la lectura de la respuesta aunque el servidor sí la procese).

**Rate limiting en login.** `Microsoft.AspNetCore.RateLimiting` (nativo de .NET 8, sin paquete NuGet propio) con una ventana fija de 5 intentos por minuto, particionada por IP (`RateLimitPartition.GetFixedWindowLimiter`) — sin particionar por IP, un solo cliente agotando el límite bloquearía el login de todos los demás usuarios, un DoS trivial autoinfligido. Aplicado solo a `POST /api/auth/login` vía `.RequireRateLimiting("login")`, no globalmente. Verificado con 7 intentos consecutivos: los primeros 5 responden 401 (credenciales), el 6º y 7º responden 429.

**Detalle de implementación no obvio:** el registro de `AddRateLimiter` vive en `Program.cs` (API), no en `Infrastructure/DependencyInjection.cs` como el resto de la configuración de seguridad (JWT, CORS) — `Microsoft.AspNetCore.RateLimiting` solo viene con el shared framework de ASP.NET Core (`Sdk.Web`), que `GestionProyectos.Infrastructure` (un class library con `Sdk` simple) no referencia por defecto. Se evaluó agregar `<FrameworkReference Include="Microsoft.AspNetCore.App" />` al `.csproj` de Infrastructure para mantener toda la configuración de seguridad en un solo lugar, pero se descartó: infla las dependencias de un class library con todo el framework web solo para una llamada, cuando la alternativa (dejar el rate limiter en la capa que ya lo tiene de forma nativa) es más simple y no rompe la separación de capas -- Infrastructure sigue sin depender de nada específico de hosting HTTP.

---

## 20. Auditoría post-entrega — Result tipado por ErrorType (fix/typed-result-errors)

Auditoría crítica solicitada explícitamente por Santiago sobre el proyecto ya completo (2026-08-02), pidiendo una revisión de evaluador senior, no de instructor. Se identificaron 5 hallazgos priorizados de más sencillo a más difícil; esta entrada documenta el primero, ya cerrado. Los otros 4 (logging pipeline behavior, Value Object `Email`, tests de integración con Testcontainers, Outbox pattern para la notificación del tablero) se documentan en sus propias entradas al implementarse.

**Problema detectado:** `Result`/`Result<T>` (`Domain/Common/Result.cs`) solo exponía `string? Error`. Cada endpoint mapeaba el status HTTP comparando el **contenido** del mensaje contra una constante del handler (`result.Error == XCommandHandler.SomeConstant ? 409 : 404`), con un `default`/`_` que caía silenciosamente en un status code (404 en la mayoría de endpoints, 400 en `ReportsEndpoints`, sin ningún criterio uniforme entre archivos). Esto significa que un `Result.Failure(nuevoMensaje)` agregado a futuro sin actualizar el switch del endpoint quedaría mal clasificado sin que el compilador, ni ningún test, lo detectaran — un bug silencioso, no una excepción.

**Decisión:** `Result`/`Result<T>` ahora exigen un `ErrorType` (enum: `NotFound`, `Conflict`, `Validation`, `Unauthorized`) en todo `Failure(...)` — no hay overload que lo omita, así que un nuevo `Failure` sin clasificar es un error de compilación, no un 404 silencioso. El mapeo a HTTP status se centralizó en `ResultExtensions.ToErrorResponse()` (API/Endpoints), con un `switch` exhaustivo por `ErrorType` que **lanza una excepción** (no cae a un default silencioso) si algún día aparece un `ErrorType` sin mapear — el fallo se vuelve ruidoso e inmediato, en vez de una respuesta HTTP incorrecta y silenciosa.

**Clasificación aplicada** (23 sitios de `Result.Failure` en 15 handlers, revisados uno a uno contra el comportamiento HTTP ya existente para no cambiar semántica, solo tiparla):
- `NotFound`: todo "entidad no encontrada" (Project, Column, Task).
- `Conflict`: reglas de negocio de estado (`ColumnHasTasks`, `ProjectHasTasks`, `DuplicateName`) y conflictos de concurrencia `xmin` (ADR §15.2).
- `Validation`: `MoveTaskCommandHandler.TargetIndexOutOfRange`, `ExportProjectReportQueryHandler.UnsupportedFormat` — antes ambos caían en el `default` de sus endpoints por rutas distintas (400 uno, 404 el otro sin querer en algún caso), ahora es explícito y uniforme.
- `Unauthorized`: `LoginCommandHandler.InvalidCredentials`.

**Por qué no una librería de terceros (ErrorOr, FluentResults):** el proyecto ya tiene un `Result` propio, simple, sin dependencias, defendible en la sustentación. Agregar una librería externa para resolver un problema de 4 categorías de error hubiera sido una dependencia nueva sin necesidad real (criterio ya aplicado contra AutoMapper, §1) — la solución tipada in-house cierra el mismo hueco con el mismo código que ya existía, solo con un campo más.

**Cobertura de regresión:** cada test de handler que ya afirmaba `result.Error == constante` ahora también afirma `result.ErrorType == ErrorType.X` — para que un futuro cambio accidental de clasificación (ej. mover `ColumnHasTasks` de `Conflict` a `NotFound` por error) rompa un test, no solo se descubra en producción. `ResultTests.cs` (Domain) cubre además que `Success()` no lleva `ErrorType` y que el tipo se preserva correctamente en `Result` (no genérico) y `Result<T>`.

---

## 21. Auditoría post-entrega — LoggingBehavior transversal (fix/logging-pipeline-behavior)

Segundo de los 5 hallazgos de la auditoría crítica (§20). La tabla de patrones (§3) afirmaba: *"Pipeline Behaviors | MediatR | Validación automática (FluentValidation) **y logging transversal** antes del Handler"*. Hasta esta entrada, solo existía `ValidationBehavior` — el logging real (Fase 7, §6.1) se implementó manualmente dentro de 5 handlers específicos (`LoginCommandHandler` + los 4 de `Tasks`), no como behavior transversal. La tabla describía una arquitectura que el código no tenía.

**Decisión:** en vez de corregir la tabla del §3 para que reflejara "logging manual selectivo", se implementó el `LoggingBehavior<TRequest, TResponse>` que la tabla ya prometía — la documentación es la fuente de verdad del proyecto (regla de `CLAUDE.md`), así que cuando diverge del código, la brecha se cierra en el código, no reescribiendo la promesa a la baja.

**Diseño:**
- `LoggingBehavior` (`Application/Common/Behaviors`) traza inicio, fin y duración de **todo** request de MediatR a nivel `Debug` — no `Information`, porque es volumen alto (una entrada por cada Command/Query, éxito o fracaso de negocio) y no aporta señal accionable por sí solo; el nivel mínimo configurable por entorno (`Serilog:MinimumLevel`, ya existente desde Fase 7) decide si se emite.
- Registrado **antes** que `ValidationBehavior` en `DependencyInjection.cs` — cada behavior envuelve al siguiente, así que Logging también traza requests que fallan validación (`ValidationException`), no solo los que llegan al Handler.
- **Nunca registra el payload del request**, solo `typeof(TRequest).Name` — evita que `LoginCommand` (contiene la contraseña en texto plano antes del hash) quede expuesto en el log si alguien agrega un `ToString()` o serialización automática a futuro. Es una decisión de diseño, no un descuido: un logging transversal genérico que serializara el request completo sería el tipo de código que un evaluador de seguridad marcaría de inmediato.
- No duplica ni reemplaza los `LogWarning` de negocio ya presentes en los 5 handlers críticos (§6.1): este behavior no conoce el resultado de negocio (`Result.IsSuccess`), solo si el pipeline se completó o lanzó una excepción no controlada — son dos niveles de logging distintos y complementarios, no redundantes. Una excepción real que llegue hasta este behavior (no las de negocio, que ya se resuelven como `Result.Failure` dentro del handler) se loguea a `Error` con el stack trace completo antes de relanzarse, sin swallowearla.

**Alternativa descartada — loguear el objeto `request` completo:** más útil para debugging ad-hoc, pero reintroduce el riesgo de PII/secretos en logs que el propio ADR ya trata como no negociable (§8: hash de contraseña, nunca en texto plano en ningún punto observable). Se descarta a favor de solo el nombre del tipo.

**Cobertura de test:** `LoggingBehaviorTests.cs` verifica el camino feliz (la respuesta de `next()` pasa sin alterarse) y que una excepción de `next()` se relanza intacta (`Assert.Same`), no envuelta ni swallowed. No se verifican las llamadas al logger en sí (`ILogger.Log<TState>` es genérico y frágil de mockear con NSubstitute) — consistente con el resto de la suite, que nunca verifica logging como comportamiento observable, solo el resultado funcional.

---

## 22. Auditoría post-entrega — Value Objects y límites de agregado (fix/value-objects-and-aggregates)

Tercero de los 5 hallazgos de la auditoría crítica (ver §20, §21). A diferencia de los dos anteriores, este no es un fix acotado a un archivo: toca las cuatro entidades de dominio, la configuración de EF Core y el límite de los agregados. Se documenta explícitamente como **corrección de deuda técnica tras una primera instancia ya validada** — el producto funcionaba de punta a punta (Fases 0-7, §1-§21) antes de esta revisión; esta entrada no es un rediseño especulativo, es el mismo criterio de "primero funciona, después se refina con la información completa del sistema real" que ya se aplicó en §14.2 (xmin diferido) y §15.2 (xmin materializado cuando el escenario real lo exigió).

### 22.1 Ausencia total de Value Objects — el hallazgo original

Con 4 entidades de dominio y el criterio ya aplicado de "sin dependencias innecesarias" (§1, descarte de AutoMapper), el proyecto nunca había introducido un Value Object propio. Tres primitivos concretos escondían invariantes reales:

- `User.Email`: un `string` normalizado (trim + lowercase) inline en el constructor, sin validación de formato -- cualquier string no vacío pasaba como "correo válido".
- `Project.StartDate`/`EndDate`: la invariante "End >= Start" estaba **copiada literalmente** en el constructor y en `Update()` -- duplicación real, no cosmética.
- `TaskEntity.Order` (LexoRank): solo validaba "no vacío" -- cualquier string pasaba como clave de orden válida, y cada consumidor debía recordar usar `StringComparer.Ordinal` para compararlas correctamente en vez de que el tipo lo garantizara.

### 22.2 Los tres Value Objects introducidos

`Domain/ValueObjects/Email.cs`, `DateRange.cs`, `LexoRankKey.cs` -- todos `sealed record` (igualdad estructural gratis), cada uno centraliza la validación que antes vivía repetida o ausente:

- **`Email`**: valida formato (`^[^@\s]+@[^@\s]+\.[^@\s]+$`) además de normalizar. `User` construye el VO en su constructor; `IUserRepository.GetByEmailAsync` pasa a aceptar `Email` en vez de `string` -- el puerto ahora exige un correo ya validado, no un string arbitrario. Esto es seguro porque `LoginCommandValidator` (FluentValidation, `.EmailAddress()`) ya garantiza formato válido antes de que el Handler construya el VO; un formato inválido nunca llega a intentar construir un `Email` en producción.
- **`DateRange`**: centraliza la invariante que antes estaba duplicada. `Project` expone `DateRange` como propiedad **derivada** (`=> new(StartDate, EndDate)`), no como columna propia -- ver §22.4 sobre por qué no se usó `ComplexProperty`.
- **`LexoRankKey`**: valida el alfabeto base62 (vía `LexoRankService.IsValidCharacter`, expuesto `internal` sin tocar el algoritmo ya probado por la única prueba nombrada explícitamente en el enunciado, sección 6.9) e implementa `IComparable<LexoRankKey>` con comparación ordinal -- los consumidores (`GetProjectBoardQueryHandler`) ya no necesitan recordar `StringComparer.Ordinal`, el tipo lo garantiza.

### 22.3 Límite de agregados -- el hallazgo más importante de los tres

`Project` exponía `Columns` y `Column` exponía `Tasks` como colecciones navegables -- decoración heredada de "consistencia de patrón" (§14.3), nunca usada por ningún Handler de Application (verificado por grep: ni las queries ni los commands navegan `project.Columns` o `column.Tasks`; `GetProjectBoardQueryHandler` arma el árbol manualmente vía `ToLookup` desde sus propios repositorios). Mientras tanto, `Column` y `TaskEntity` ya tenían repositorio propio y `TaskEntity` ya tenía su propio token de concurrencia (`xmin`, §15.2) -- el código ya se comportaba como **tres agregados independientes**, pero el modelo de objetos fingía ser **un solo agregado** navegable desde `Project`.

**Alternativa evaluada y descartada -- colapsar a un solo agregado real (Project como raíz única):** habría significado que `Column`/`TaskEntity` pierdan su repositorio propio, y que un único `xmin` en `Project` proteja todo el árbol. Se descartó porque **rompería la edición concurrente de grano fino que 6.6/6.7 exige probar**: con un solo token de concurrencia por proyecto, dos usuarios moviendo tareas *distintas* del mismo proyecto competirían por el mismo `xmin` y generarían un conflicto de concurrencia falso -- el escenario de colaboración simultánea que el propio enunciado pide validar dejaría de funcionar como corresponde. Aplicar DDD "de libro" sin considerar que este sistema necesita mutaciones concurrentes de grano fino habría sido el tipo de sobre-ingeniería que este proyecto evita consistentemente en otras decisiones (§1).

**Decisión:** se quitaron `Project.Columns` y `Column.Tasks` del dominio (nunca se usaron) y se reconfiguró la relación FK en EF Core como unidireccional (`HasOne<Project>().WithMany()`, sin navegación) -- el modelo de objetos ahora refleja honestamente lo que la persistencia ya hacía. `Project`, `Column` y `TaskEntity` quedan documentados explícitamente como **tres agregados independientes**, cada uno con su propio repositorio y su propio límite de concurrencia, decisión consciente para preservar la edición concurrente de grano fino.

### 22.4 Detalle no obvio -- `ComplexProperty` (EF Core 8) no soporta `HasData`

El primer intento de mapear `DateRange` fue `builder.ComplexProperty(p => p.DateRange, ...)`, la forma idiomática de EF Core 8 para Value Objects sin identidad ni tabla propia. Falló en tiempo de diseño: *"Complex properties are currently not supported in seeding"* ([dotnet/efcore#31254](https://github.com/dotnet/efcore/issues/31254)), y el enunciado (sección 6.2) exige migración semilla -- no es una opción descartable.

**Decisión:** `Project.StartDate`/`EndDate` siguen siendo las columnas planas mapeadas de siempre; `DateRange` es una propiedad derivada de solo lectura (`=> new(StartDate, EndDate)`), ignorada explícitamente por EF (`builder.Ignore(p => p.DateRange)`). El VO sigue centralizando la validación (el constructor y `Update()` de `Project` construyen un `DateRange` transitorio solo para validar, antes de asignar `StartDate`/`EndDate`), pero la persistencia no cambia. Corolario verificado: por ser una propiedad C# calculada, `DateRange` **no es traducible a SQL** -- `ProjectReportRepository` (la consulta LEFT JOIN de §18) debe seguir proyectando `p.StartDate`/`p.EndDate` directamente, nunca `p.DateRange.Start`, o EF lanza en tiempo de ejecución al no poder traducir la expresión.

`Email` y `LexoRankKey`, en cambio, sí usan `HasConversion` (VO de una sola propiedad, sin este problema) -- `HasConversion` sí soporta `HasData` con normalidad, verificado por la migración de comprobación (`CheckValueObjectsMapping`) generada sin error.

### 22.5 Verificación real, no solo unitaria

Los mocks de repositorio en los tests unitarios nunca ejercitan el modelo real de EF Core. Antes de cerrar este punto, se levantó el stack completo (`docker compose up`) contra Postgres real y se verificó con `curl`: login (`Email` VO + JWT), listado y tablero de un proyecto real (`DateRange` + `LexoRankKey` serializados correctamente), mover una tarea (recalcula una `LexoRankKey` nueva, respeta `xmin`), creación de proyecto con nombre duplicado (409, `ErrorType.Conflict`) y login con contraseña incorrecta (401, `ErrorType.Unauthorized`) -- confirmando que el límite de agregados independientes no rompió el ensamblado del tablero (`GetProjectBoardQueryHandler` sigue construyendo el árbol sin la navegación eliminada) y que la migración de verificación (`CheckValueObjectsMapping`, `Up`/`Down` vacíos -- mismo patrón que la migración vacía de `xmin`, §15.2) se aplica sin error sobre una base de datos real.

---

## 23. Auditoría post-entrega — Tests de integración con Testcontainers (fix/integration-tests-testcontainers)

Cuarto de los 5 hallazgos de la auditoría crítica (ver §20-§22). La verificación manual del §22.5 (y, antes, la verificación manual que el propio §18.2 admite haber hecho "a mano" para el `LEFT JOIN` del reporte, y el §15.2 para `xmin`) demuestra que este proyecto sí valida comportamiento real contra Postgres -- pero nunca queda protegido de regresión: la próxima vez que alguien toque esa consulta o esa configuración, nadie vuelve a verificarlo a mano por defecto.

**Decisión:** nuevo proyecto `GestionProyectos.IntegrationTests` (xUnit + `Testcontainers.PostgreSql`), agregado a `GestionProyectos.sln`. Un solo contenedor Postgres real compartido por toda la suite (`ICollectionFixture`, no un contenedor por test -- levantar el contenedor es el costo caro, correr las migraciones sobre él no), con las migraciones reales aplicadas (`Database.MigrateAsync()`, incluida la seed data vía `HasData`) antes de correr cualquier test.

**Tres tests, elegidos por lo que un mock no puede probar, no por cobertura numérica** (mismo criterio de riesgo que ya rige la suite unitaria, §2 METODOLOGIA):

1. `ProjectReportRepositoryTests`: confirma en código lo que §18.2 decía haber verificado a mano -- un proyecto sin tareas produce 1 fila con campos de tarea en `null`, no 0 filas (que sería indistinguible de "el proyecto no existe"). El `LEFT JOIN` encadenado (`Project -> Columns -> Tasks -> User`) solo se puede probar de verdad contra el motor real.
2. `TaskConcurrencyTests`: confirma el conflicto de `xmin` (§15.2) con dos `AppDbContext` reales modificando la misma tarea -- y, en el mismo archivo, confirma la contraparte que justifica el diseño de agregados independientes (§22.3): dos tareas *distintas* del mismo proyecto no compiten por el mismo token y ambas guardan sin conflicto. `xmin` es una columna de sistema gestionada por Postgres; ningún mock la reproduce.
3. `ProjectRepositoryTests`: confirma el filtro `ILike` + índice GIN `pg_trgm` (sección 6.3, §9) con coincidencia parcial e insensible a mayúsculas -- un provider en memoria no tiene la extensión `pg_trgm` instalada.

**Efecto colateral aceptado en CI:** al agregar el proyecto al `.sln`, `dotnet test GestionProyectos.sln` (usado por `.github/workflows/ci.yml`) ahora corre ambas suites juntas. Se acepta porque los runners de GitHub Actions (`ubuntu-latest`) traen Docker disponible por defecto -- el mismo mecanismo que ya usa Testcontainers localmente -- sin pasos adicionales de configuración. No se filtró la suite de integración fuera de CI (por ejemplo, con un trait/categoría y un job separado) porque habría sido complejidad anticipada para un problema que, hasta que se demuestre lo contrario tras el primer run de CI post-merge, no existe: el runner soporta Docker nativamente y Testcontainers está diseñado exactamente para este entorno.

**Alcance descartado -- filtrar por trait o mover a un job de CI separado:** se evaluó marcar los tests de integración con un `[Trait("Category", "Integration")]` y excluirlos del job principal de CI, corriéndolos en un job aparte con más tiempo de timeout. Se descartó por ahora: añade un segundo job de CI, un paso de configuración adicional, y resuelve un problema (tiempo de CI, aislamiento de fallos) que aún no se ha observado -- si el primer run de CI post-merge muestra que Testcontainers no funciona limpiamente en el runner o que el tiempo de CI se vuelve inaceptable, se revisita esta decisión y se documenta el cambio aquí, siguiendo el mismo criterio de "no resolver un problema hipotético antes de confirmarlo" que ya rige otras decisiones de este documento (por ejemplo, la revocación por `jti` en vez de refresh tokens, §16).

---

## 24. Auditoría post-entrega — Outbox Pattern para la notificación del tablero (fix/board-notification-outbox)

Quinto y último de los 5 hallazgos de la auditoría crítica (ver §20-§23). El hallazgo original (§20, primera auditoría): `CreateTaskCommandHandler`/`UpdateTaskCommandHandler`/`DeleteTaskCommandHandler`/`MoveTaskCommandHandler` llamaban a `IBoardNotifier` directamente **después** de `SaveChangesAsync`. Si el proceso crasheaba entre el commit y la notificación por SignalR (timeout de red, reinicio del contenedor, GC pause), el cambio quedaba persistido pero ninguna otra sesión se enteraba -- el tablero de otro usuario quedaba desincronizado hasta un refresh manual.

### 24.1 Domain Events -- alternativa evaluada y descartada, con un motivo concreto

Antes de implementar, se analizó si la forma idiomática de resolver esto era que la entidad (`TaskEntity`) levantara un **Domain Event** (`TaskMovedDomainEvent`, etc.) en vez de que el Handler encolara el evento explícitamente. Es la respuesta "de libro" en DDD, y hubiera sido la decisión correcta **si** este proyecto tuviera o previera múltiples consumidores del mismo hecho de negocio -- hoy hay exactamente uno (SignalR).

**El motivo concreto para descartarla, no solo "menos código":** el mecanismo de exclusión del emisor (`ConnectionId` del socket SignalR que ya aplicó el cambio de forma optimista, §15.3) es un dato de transporte/presentación, no de negocio -- un evento de dominio puro no debería conocer qué es un `ConnectionId` de SignalR, por la misma regla que ya rige el resto del proyecto ("el dominio no conoce EF Core ni ningún framework externo", §2). Si el evento de dominio no puede cargar el `ConnectionId`, el Handler de todos modos tiene que pasar ese dato por otro lado al encolar -- la "garantía estructural" de Domain Events (imposible olvidar levantar el evento) se rompe parcialmente, porque el `ConnectionId` sigue siendo responsabilidad manual del Handler de cualquier forma. Se opta por el Handler orquestando el encolado explícitamente (Opción A), documentando Domain Events como la decisión correcta si algún día aparece un segundo consumidor real del mismo evento -- mismo patrón que otras decisiones de este documento que se difieren hasta que el escenario real las dispare (§14.2 → §15.2, `xmin`).

### 24.2 RabbitMQ -- descartado por segunda vez, mismo motivo

Se evaluó nuevamente introducir un bus de mensajería para el mecanismo de despacho del Outbox (colas reales en vez de polling). Se descartó otra vez: este deployment es de una sola instancia de API (§1 ya descarta el AppHost de Aspire y YARP por la misma razón -- "no hay múltiples servicios que enrutar"), y la ventaja real de un broker (coordinación entre réplicas) no tiene consumidor en la arquitectura actual. Meter un broker ahora para un problema que Postgres + Outbox ya resuelve sin infraestructura nueva habría contradicho el patrón de decisión que este mismo documento aplica en `§1` (RabbitMQ), `§1` (Aspire AppHost), `§1` (YARP) y `§7.2` (hexagonal literal en frontend).

### 24.3 Diseño: polling + señal in-process, con `FOR UPDATE SKIP LOCKED` como seguro barato

- **`OutboxMessage`** (`Infrastructure/Persistence/Entities`, no Domain -- mismo criterio que `RevokedToken`, §16): registro técnico sin invariantes de negocio propias.
- **`IOutboxWriter.Enqueue(...)`** (Application, puerto): el Handler lo llama **antes** de `SaveChangesAsync`, nunca invoca `SaveChanges` por sí mismo -- entra en la misma transacción que el cambio de negocio. Si el `SaveChanges` posterior falla (ej. conflicto de `xmin`), la fila de outbox se revierte junto con el cambio de negocio: la atomicidad la da la transacción de Postgres, no un mecanismo a medida.
- **`OutboxProcessor`** (Infrastructure): el "qué hace un ciclo" -- reclama un lote con `SELECT ... FOR UPDATE SKIP LOCKED`, lo marca procesado dentro de la misma transacción corta (sin mantener el lock de fila abierto durante la llamada de red a SignalR), y despacha cada mensaje a `IBoardNotifier` según su tipo. Separado deliberadamente de `OutboxDispatcher` (el "cuándo corre un ciclo" -- `BackgroundService` + polling) para que sea testeable directamente contra Postgres real sin levantar todo el hosted service.
- **`OutboxDispatcher`** (Infrastructure, `BackgroundService` + `IOutboxSignal`): polling cada 1s + un `Channel` para que el Handler despierte el ciclo inmediatamente tras un `SaveChanges` exitoso (`IOutboxSignal.Signal()`), sin esperar el próximo tick. Verificado en vivo (`docker compose` + `curl`, moviendo una tarea real): el evento se encoló, se despachó y quedó marcado `processed_at_utc` en **63ms** -- la señal in-process funciona, no solo el polling de respaldo.
- **`FOR UPDATE SKIP LOCKED`**: insurance barata para si algún día se escala horizontalmente -- sin esto, dos instancias de la API podrían reclamar y notificar el mismo evento duplicado. Hoy el deployment es de una sola instancia (§24.2), así que esto no protege contra un problema actual, pero cuesta una cláusula SQL, no una dependencia nueva.

**Trade-off aceptado y documentado, no descubierto en producción:** el mensaje se marca procesado en la misma transacción que el claim, **antes** de intentar el dispatch real a SignalR (no después). Si el proceso crashea entre el claim y el dispatch efectivo (ventana de milisegundos, dentro de un solo ciclo), ese mensaje específico se pierde -- mucho más angosto y raro que el bug original (que podía afectar prácticamente cualquier request, dependiendo del timing). Se acepta este trade-off en vez de la alternativa (marcar procesado después del dispatch, con un job de recuperación de "claims abandonados") por el mismo criterio de gestión de riesgo bajo restricción de tiempo que ya se aplicó en otras decisiones de este documento (ej. BCrypt sobre Argon2, §11.1; sin limpieza periódica de `revoked_tokens`, §16).

### 24.4 Detalle no obvio -- columna `Id` sin `snake_case` en el primer intento

La primera migración generada (`AddOutboxMessages`) creó la columna de la clave primaria como `"Id"` (con mayúscula, entre comillas) en vez de `id` -- porque, a diferencia de las demás propiedades de `OutboxMessageConfiguration`, no se llamó `builder.Property(m => m.Id).HasColumnName("id")` explícitamente (mismo patrón que sí se aplicó correctamente en `RevokedTokenConfiguration` para `Jti`→`jti`). Se detectó al inspeccionar la tabla real con `psql` durante la verificación en vivo (§24.3), no en revisión de código -- la migración se regeneró (`dotnet ef migrations remove` + `add`) antes de hacer commit, así que nunca llegó a existir una migración pública con el nombre inconsistente que hubiera que corregir con una migración adicional después.

### 24.5 Cobertura de test

- Unitarios: los 4 Handlers de Tasks mockean `IOutboxWriter`/`IOutboxSignal` en vez de `IBoardNotifier`. La aserción de "no se notifica tras un conflicto de concurrencia" se corrigió para verificar `IOutboxSignal.DidNotReceive().Signal()` en vez de `IOutboxWriter.DidNotReceive().Enqueue(...)` -- en el diseño real, `Enqueue` puede llamarse antes del `SaveChanges` que falla (la atomicidad la da la transacción de BD, no el mock), pero `Signal()` solo se llama después de un `SaveChanges` exitoso, que es la garantía que de verdad importa verificar.
- Integración (`GestionProyectos.IntegrationTests`, Testcontainers): atomicidad real de `Enqueue` + `SaveChanges` contra Postgres; `OutboxProcessor.ProcessPendingAsync` reclama, marca procesado y despacha correctamente; un mensaje ya procesado no se vuelve a despachar en un segundo ciclo.
- Verificación en vivo (`docker compose` + `curl` + `psql`): confirmada en §24.3.

---

## 25. Colección de Postman completa + coherencia de documentación (fix/postman-collection-coverage)

Trabajo diferido explícitamente hasta después de cerrar los 5 puntos de la auditoría crítica (§20-§24). Dos partes, encontradas y resueltas en la misma pasada porque una llevó a la otra.

### 25.1 Cobertura completa de la colección

La colección solo cubría Auth, Projects y Columns. Se agregaron los folders **Users**, **Board**, **Tasks** (create/update/move/delete, con sus casos de error) y **Reports** (PDF/Excel/formato inválido), y **Logout** al final de `Cleanup` -- deliberadamente el último request de toda la colección, porque revoca el token real (blocklist por `jti`, §16) y cualquier folder posterior lo necesitaría. `Delete Column` se movió de `Columns` a `Cleanup` por la misma razón de orden: correr después de `Tasks > Delete Task` para no chocar con la regla de negocio de la sección 6.4 (no borrar columna con tareas).

**Verificado con Newman, no solo importado a Postman:** `npx newman run` sobre la colección completa reveló dos bugs reales antes de darla por cerrada:

1. **Fechas hardcodeadas ya vencidas.** `Create Project`/`Update Project` usaban `"2026-01-01"` como `startDate` -- válido cuando se escribió la colección, inválido para la regla de negocio "la fecha de inicio no puede ser anterior a hoy" en cualquier corrida posterior a esa fecha. Rompía la colección completa en cascada (sin `projectId`, ningún request posterior podía encadenar). Se corrigió con un pre-request script en `Create Project` que calcula `futureStartDate`/`futureEndDate` relativos a "ahora" -- una fecha fija se vuelve a romper con el tiempo; una calculada, no.
2. **GUID vacío en el caso 404 de `Create Task`.** `CreateTaskCommandValidator.ColumnId` tiene una regla `.NotEmpty()` que rechaza `Guid.Empty` (`00000000-...`) con 400 (validación) antes de llegar al Handler -- el caso quería probar el 404 de negocio ("columna no encontrada"), no el 400 de formato. Se corrigió usando un GUID no-vacío pero inexistente (`11111111-...`).

Ninguno de los dos es un bug del código de producción -- son bugs de la colección de pruebas, encontrados exactamente por la razón por la que se corre con Newman antes de cerrar el trabajo (mismo criterio que motivó el bug de `status` vacío documentado en el README §11, Fase 6).

### 25.2 Incoherencia real encontrada al repasar: §20/§21 fuera de orden físico

Al revisar si la tabla de Patrones de Diseño (§3) y la fila de RabbitMQ (§1) mencionaban el Outbox Pattern (no lo hacían -- corregido en el PR anterior, fix/board-notification-outbox), se encontró un problema más serio en este mismo archivo: las entradas **§20 y §21 estaban numeradas correctamente pero ubicadas físicamente después de §22, §23 y §24** -- es decir, alguien leyendo el archivo de arriba a abajo encontraba §22→§23→§24→§20→§21, no la secuencia 20→21→22→23→24 que los números prometen. Causa: cada entrada nueva se agregó con un `Edit` que insertaba texto justo antes del final del archivo tal como estaba en ese momento, y en algún punto una entrada se insertó en el lugar equivocado sin que nadie lo notara hasta esta revisión explícita.

**Decisión:** reordenar físicamente el archivo para que la posición coincida con la numeración (§20 → §21 → §22 → §23 → §24), sin cambiar una sola palabra del contenido de cada entrada -- es un problema de orden, no de contenido. Se documenta aquí porque es exactamente el tipo de incoherencia que este documento existe para prevenir, y porque fue Santiago quien la detectó pidiendo explícitamente "un último repaso a los docs para evitar estas incoherencias antes de hacer el merge" -- no una revisión que el asistente propuso por iniciativa propia.

### 25.3 README también actualizado

`README.md` §2 gana instrucciones más específicas (tiempo de primer arranque, cómo verificar `/health`, cómo detener y limpiar el volumen, cómo importar y correr la colección completa desde Postman o via `newman` en línea de comandos) y §5/§6/§10 se corrigen para dejar de describir un sistema que ya no existe (los Handlers de Tasks ya no llaman a `IBoardNotifier` directamente, el conteo de tests de integración no incluía los 4 de `OutboxProcessorTests`).

---

## 26. Auditoría de frontend y seguridad general (fix/frontend-security-hardening)

Revisión pedida explícitamente por Santiago con foco en frontend y seguridad transversal (no solo backend, ya auditado en §19-§24), con instrucción explícita de "nada de camino feliz". Se identificaron hallazgos reales y se corrigieron en la misma sesión (no solo se documentaron) porque ninguno requería una decisión de arquitectura mayor salvo el punto §26.4.

### 26.1 Sin cabeceras de seguridad HTTP — el hallazgo más serio

Ni `nginx.conf` ni la API emitían `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy` ni `Permissions-Policy`. La única defensa contra XSS era el auto-escape de Angular en interpolación (verificado: cero usos de `[innerHTML]`/`bypassSecurityTrust*` en todo `frontend/src`) — sin CSP, esa era la única capa, y sin `X-Frame-Options`/`frame-ancestors`, el login era embebible en un `<iframe>` de un sitio malicioso (clickjacking).

**Decisión:** cabeceras agregadas en `frontend/nginx.conf` (único punto de entrada, §1). CSP con `script-src 'self'` y `style-src 'self' 'unsafe-inline'` (Sakai/PrimeNG usa estilos inline en varios templates; una CSP que los bloqueara habría roto la UI). `connect-src` incluye `ws:`/`wss:` porque SignalR negocia el handshake por WebSocket bajo el mismo origen. `frame-ancestors 'none'` cierra el clickjacking sin depender de `X-Frame-Options` (redundante a propósito para navegadores viejos que no leen CSP).

**Efecto secundario no anticipado — la CSP rompió toda la aplicación, y cómo se diagnosticó.** Al levantar el stack con `docker compose`, la aplicación entera (empezando por el login) se renderizó sin ningún estilo. La causa: la optimización `inlineCritical` de Angular, **activa por defecto en builds de producción**, difiere la carga del CSS emitiendo `<link rel="stylesheet" media="print" onload="this.media='all'">`. Ese `onload` es un **manejador de evento inline**, que `script-src 'self'` bloquea por definición — las hojas quedaban permanentemente en `media="print"` y nunca se aplicaban. Solo se manifestaba en el build de producción: `ng serve` no aplica `inlineCritical`, así que la verificación en el servidor de desarrollo pasó limpia y dio una falsa sensación de seguridad.

Se resolvió desactivando `inlineCritical` en `frontend/angular.json` (`optimization.styles.inlineCritical: false`), no agregando `'unsafe-inline'` a `script-src`: esa segunda opción habría anulado justamente la protección que motivó la cabecera, a cambio de un beneficio marginal de *first contentful paint* en una aplicación de evaluación.

**Lección de método, registrada a propósito:** el diagnóstico inicial fue incorrecto dos veces (se culpó primero a la CSP sin pruebas, luego al refactor de `shared/` del §26.4, que era inocente). Ambos errores tuvieron la misma raíz: se verificó en el navegador **sin controlar la caché**, y el navegador estaba ejecutando un `main.<hash>.js` de un build anterior. Recién al comparar los hashes servidos por nginx contra los cargados en la página se detectó la discrepancia. El diagnóstico definitivo se hizo midiendo geometría real (`getBoundingClientRect` sobre cinco elementos) contra el build de `main` servido en el mismo puerto y con la caché forzada a saltarse, no por inspección visual — ver §26.9 sobre el bug de caché que hizo posible esta confusión.

### 26.2 CORS más ancho de lo necesario

`AllowAnyHeader().AllowAnyMethod()` en `DependencyInjection.cs` — el origen ya estaba restringido a whitelist (§19), pero headers/métodos no. Acotado a `Authorization`/`Content-Type` y a los verbos REST que la API realmente expone (`GET/POST/PUT/DELETE/OPTIONS`). No es una vulnerabilidad explotable per se (el origen ya filtraba), pero es endurecimiento barato.

### 26.3 Comentario de código que contradecía la decisión real

`realtime-board.service.ts` afirmaba en un comentario que el JWT "vive solo en memoria", contradiciendo la decisión ya revisada en §17 (`sessionStorage`, desde Fase 4). Corregido. Es exactamente el tipo de divergencia documentación-código que §21 ya trató como no negociable, aplicado aquí a un comentario en vez de a un ADR.

### 26.4 `shared/` — la capa documentada en §2.1 no existía en el código

El ADR §2.1 promete `core/shared/features` en el frontend para cumplir la sección 4 del enunciado ("separación por capas... en el frontend"). El árbol real solo tenía `core/`, `features/` y `layout/` — `shared/` nunca se creó, y había duplicación real que debía vivir ahí:

- `<p-toast>`/`<p-confirmDialog>` copiados literalmente en `board.component.html` y `project-list.component.html`, cada uno con su propio `providers: [ConfirmationService, MessageService]` a nivel de `@Component` (una instancia de cada servicio por página).
- `priorityOptions = Object.values(TaskPriority).map(...)` derivado de forma idéntica en `board.component.ts` y `task-form.component.ts`.

**Decisión (confirmada explícitamente por Santiago antes de implementar, regla del flujo obligatorio de `CLAUDE.md`):** se creó `frontend/src/app/shared/` con `AppNotificationsComponent` (host único de `p-toast`/`p-confirmDialog`) declarado en un nuevo `SharedModule`, montado una sola vez en `app.component.html` (raíz de la app, fuera de cualquier página). `ConfirmationService`/`MessageService` pasan a proveerse una sola vez en `AppModule` (antes: una instancia nueva por cada componente que los declaraba) — así todo componente que los inyecta (incluidos los hijos `TaskFormComponent`/`ProjectFormComponent`/`ProjectColumnsComponent`, que nunca los proveían y ya resolvían desde el ancestro más cercano) apunta a la misma instancia que el host raíz escucha. `priorityOptions` se centralizó como `TASK_PRIORITY_OPTIONS` en `task.model.ts`.

**Alternativa descartada — dejar `shared/` sin crear y corregir solo la duplicación puntual:** se descarta porque el gap real no era la duplicación en sí (dos líneas, bajo costo) sino que el ADR documenta una capa arquitectónica exigida por el enunciado que el código nunca tuvo — exactamente el criterio que ya se aplicó en §21 ("cuando la documentación diverge del código, la brecha se cierra en el código, no reescribiendo la promesa a la baja").

**No se tocó** (evaluado y descartado explícitamente, mismo criterio de no sobre-ingeniería que ya rige el resto del proyecto): migración a standalone components (Angular 17 lo permite, pero es una reescritura de superficie amplia sin beneficio funcional) y un state management centralizado tipo NgRx (con una sola vista consumiendo tiempo real, sería la misma sobre-ingeniería que §15.4 ya descartó para el servicio de SignalR).

### 26.5 Login sin validación de cliente — inconsistencia de arquitectura de formularios

`LoginComponent` era el único componente de la app en usar template-driven forms (`[(ngModel)]`, sin `required` ni validación de formato), mientras `TaskFormComponent`/`ProjectFormComponent` ya usaban Reactive Forms. No era una falla de seguridad (el backend ya valida con FluentValidation), pero un formulario que podía enviarse vacío y una inconsistencia de patrón no justificada en ningún punto del ADR. Migrado a Reactive Forms (`FormBuilder`, `Validators.required`/`.email`), consistente con el resto de la app.

### 26.6 Bug preexistente encontrado al verificar en navegador, no parte de la auditoría original

Al levantar `ng serve` para verificar los cambios anteriores, la consola mostraba `NG0303: Can't bind to 'ngIf'` en `AppComponent` — `AppModule` nunca importaba `CommonModule`, así que el `*ngIf="loading"` del overlay de carga entre navegaciones (`app.component.html`) nunca funcionó. No relacionado con los hallazgos de seguridad; se corrigió por ser un bug real y barato de arreglar, encontrado por seguir la disciplina de verificación en navegador antes de dar un cambio de frontend por cerrado.

### 26.7 Alcance sin ACL por proyecto — no es un hallazgo, es scope explícito

No existe ningún control de propiedad/rol: cualquier usuario autenticado puede ver y mutar cualquier proyecto/tablero. Contrastado contra el PDF del enunciado (sección 6.2: "todos los endpoints de negocio necesarios protegidos con autorización", sección 5: modelo de dominio mínimo sin roles) — el enunciado nunca pide multi-tenancy. Es coherente con "tablero compartido de equipo", así que no se implementó ningún cambio; se documenta aquí para que quede una respuesta lista si surge en la sustentación, en vez de que parezca un descuido no evaluado.

### 26.8 No implementado — trade-off de rate limiting por IP, documentado, no rediseñado

El rate limit de login (§19, 5/min por IP) protege contra DoS auto-infligido, pero también significa que usuarios legítimos detrás del mismo NAT/proxy corporativo comparten cupo. No se cambió a bloqueo por cuenta porque es un rediseño real (requiere lockout por usuario + reglas de desbloqueo) no solicitado y desproporcionado para 2 usuarios semilla — se deja documentado como limitación conocida, no como pendiente.

### 26.9 `Cache-Control` ausente en nginx — bug real encontrado depurando lo anterior

Perseguir el fallo del §26.1 destapó un bug independiente y más serio para el evaluador: `nginx.conf` no enviaba **ninguna** cabecera `Cache-Control`, solo `ETag`/`Last-Modified`. Con eso el navegador aplica caché heurística sobre `index.html` — el único archivo del build **sin hash en el nombre**, y precisamente el que referencia a todos los bundles hasheados. Efecto observado en vivo, no teórico: el navegador ejecutaba un `main.<hash>.js` que ya no existía en el servidor, sirviendo una SPA obsoleta completa después de un redespliegue. Al evaluador le pasaría lo mismo al recargar tras cualquier cambio.

**Decisión:** `index.html` pasa a `no-cache, must-revalidate`; el resto de assets, que llevan hash de contenido (`outputHashing: all`, o sea que si cambia el contenido cambia la URL), a `immutable` con un año.

**Detalle no obvio de nginx:** se implementó con un `map` a nivel `http` y un único `add_header Cache-Control $cache_control` a nivel `server`, **no** con un `add_header` dentro de cada `location`. Motivo: en nginx, un `add_header` en un bloque hijo **descarta todas** las cabeceras heredadas del padre — hacerlo por `location` habría dejado silenciosamente las cinco cabeceras de seguridad del §26.1 fuera de las respuestas de `index.html` y de los assets, es decir, habría desactivado el hardening justo donde más importa. Verificado con `curl` que ambas rutas devuelven `Cache-Control` **y** la CSP.

**Regresión introducida por esta misma decisión, detectada después y corregida.** La herencia de cabeceras que motivó el `map` opera en *todos* los `location`, no solo en los de contenido estático: `location /api/` y `location /hubs/` tampoco declaran `add_header` propio, así que heredaban el `Cache-Control` del `server` y, al no coincidir con ninguna regex del `map`, caían en el `default`. Resultado: **cada respuesta de la API se servía con `public, max-age=31536000, immutable`** — un año de caché inmutable sobre datos de negocio. La verificación original con `curl` (arriba) solo cubrió `index.html` y un asset hasheado; nunca se probó una ruta del proxy, y ahí estaba el fallo.

Se manifestó como un `404 Not Found` en `PATCH /api/tasks/{id}/move` sobre una tarea que el propio Santiago había borrado minutos antes: el navegador pintaba un tablero de dos horas atrás sin volver a preguntar. El diagnóstico se cerró por evidencia, no por hipótesis — el log de la API mostraba el `Warning` "Intento de mover la tarea inexistente" (o sea, ruta y JWT correctos, 404 de aplicación), la fila tenía `is_deleted = true` junto con su columna y su proyecto, y el conteo de rutas del log no registraba **ningún** `GET` de tablero tras el reinicio: solo el login entraba, porque es `POST` y los `POST` no se cachean. `immutable` agrava el cuadro: instruye al navegador a no revalidar **ni con F5**, así que el fix del servidor no basta y hay que limpiar la caché ya emitida.

Más grave que el bug funcional es el lado de seguridad: `public` autoriza a cualquier proxy compartido a almacenar respuestas de un usuario autenticado y servirlas a otro. Entra en el checklist OWASP de `METODOLOGIA.md` §9.3 y no se había contemplado.

**Decisión:** `~^/api/` y `~^/hubs/` se agregan al `map` con `no-store`. Se mantiene el `map` en vez de un `add_header` por `location` por el mismo motivo de herencia de arriba — la alternativa habría vuelto a apagar las cabeceras de seguridad, esta vez en las respuestas de la API. `no-store` y no `no-cache`: son datos autenticados y no deben quedar en disco, ni siquiera para revalidar. `/hubs/` se incluye aunque el handshake de SignalR sea `POST`, porque si el WebSocket no está disponible el cliente degrada a **long-polling por `GET`** y un `max-age` de un año dejaría el tiempo real muerto de una forma difícil de diagnosticar.

**Lección de método (segunda de esta misma rama, misma raíz):** la verificación del §26.9 falló por comprobar solo las rutas que el cambio pretendía afectar, no todas las que la directiva alcanzaba. En nginx, el alcance real de una directiva heredada es el `server` completo — cualquier verificación de cabeceras tiene que cubrir una ruta de cada `location`, incluidos los de proxy.

### 26.10 Verificación

- `ng test --watch=false`: **74/74** specs.
- `dotnet build GestionProyectos.sln`: sin errores ni warnings.
- **Cabeceras `Cache-Control` por clase de ruta**, tras la regresión del §26.9, con `curl` contra el stack real y cubriendo **un `location` de cada tipo**: `/` y `/index.html` → `no-cache, must-revalidate`; `main.<hash>.js` → `public, max-age=31536000, immutable`; `/api/projects` y `/hubs/board/negotiate` → `no-store`. En las cinco, las cinco cabeceras de seguridad del §26.1 siguen presentes (la herencia no se rompió). Confirmado también sobre un `200` autenticado real, no solo sobre el `401`: `GET /api/projects` con `Bearer` válido devuelve `no-store` y **solo** el proyecto semilla, es decir, los proyectos borrados que el navegador seguía mostrando venían exclusivamente de su caché.
- **Verificación end-to-end contra el stack real de `docker compose`** (no solo `ng serve`, precisamente por la lección del §26.1), con la caché del navegador forzada a saltarse en cada medición: geometría del login idéntica a la de `main` (`getBoundingClientRect` sobre cinco elementos); validación de cliente bloqueando el submit sin emitir petición HTTP; login completo hasta `/projects`; layout Sakai correcto; `app-notifications`/`p-toast`/`p-confirmDialog` presentes **exactamente una vez** en el DOM; diálogo de confirmación abriéndose desde el servicio singleton y cancelándose sin borrar datos; tablero renderizando columnas y tarjetas; y **SignalR conectando bajo la CSP** — confirmado por el indicador de presencia poblado, que exige WebSocket activo, `JoinBoard` y evento `BoardPresenceChanged` recibido. Cero errores y cero violaciones de CSP en consola.
