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
| Borrado de Proyecto con contenido | Hard delete en cascada dentro de transacción (Tareas → Columnas → Proyecto), con confirmación explícita en UI | Soft-delete añade filtros globales de query sin estar exigido; se documenta el trade-off |
| Dónde vive el cálculo de posición | Backend autoritativo (LexoRank); frontend reordena array localmente para optimistic update | Evita duplicar lógica de negocio crítica en dos lenguajes |
| Almacenamiento del JWT en cliente | Memoria (variable de servicio Angular), no localStorage ni httpOnly cookie | El enunciado (6.2) exige un interceptor que **adjunte** el token manualmente — una cookie httpOnly se envía automáticamente y no requeriría interceptor, por lo que el propio diseño del enunciado indica token accesible desde JS |
| Mapeo Entity↔DTO | Manual, centralizado en métodos de extensión por entidad | Explícito y defendible, sin duplicación inline en cada Handler |
| Diagrama de base de datos | Imagen PNG (ERD) embebida en README, generada desde el esquema real de las migraciones | Cumple la restricción explícita de 8: "imagen solamente, no textos de otras herramientas externas" |

---

## 8. Seguridad Adicional (no exigida explícitamente, pero cubierta por el criterio de "código seguro" de sección 2)

- CORS con whitelist explícita del origen del frontend (no `AllowAnyOrigin`).
- Rate limiting en el endpoint de login (`Microsoft.AspNetCore.RateLimiting` nativo de .NET 8).
- Validación explícita de `Issuer`, `Audience` y expiración en la configuración de `AddJwtBearer`.
- Sanitización de contenido de usuario en Angular (evitar `[innerHTML]` sin sanitizar en descripciones de tarea).
- Hash de contraseña con salt + pepper (algoritmo lento tipo BCrypt/Argon2, no SHA256 puro).

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
