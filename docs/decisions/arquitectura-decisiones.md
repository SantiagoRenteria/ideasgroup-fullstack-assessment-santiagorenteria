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
| RabbitMQ | El requisito de tiempo real se resuelve completo con SignalR. RabbitMQ solo se justificaría para notificaciones persistentes offline, que no están pedidas. Se documenta como extensión futura (outbox pattern) sin implementar. |
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
| Pipeline Behaviors | MediatR | Validación automática (FluentValidation) y logging transversal antes del Handler |
| Result Pattern | Domain/Application | Errores de negocio previsibles sin abusar de excepciones. Convención: `Result.Failure` → 400/409 según tipo de error, documentado por caso |
| Strategy + Factory | Módulo de reportes | Exportadores PDF/Excel intercambiables desde DTO común (ver sección 5) |
| Repository + Unit of Work | Infrastructure | Puerto de persistencia; dominio ignora EF Core |
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

- **Serilog**: logging estructurado en JSON con propiedades contextuales (`UserId`, `ProyectoId`).
- **OpenTelemetry SDK** en la API, exportando trazas/métricas/logs vía OTLP hacia **Aspire Dashboard standalone** (contenedor único en docker-compose, sin AppHost).
- Sin puntos directos en el rubro de evaluación; se incluye como buena práctica de bajo riesgo de infraestructura, priorizado por debajo de todo lo obligatorio.

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
