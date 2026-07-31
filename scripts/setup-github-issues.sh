#!/usr/bin/env bash
# Crea labels, milestones e issues del backlog inicial en GitHub.
#
# Requisitos previos:
#   1. gh CLI instalado: https://cli.github.com
#   2. Autenticado: `gh auth login`
#   3. El repositorio ya debe existir en GitHub (crear vacío o con este mismo código ya pusheado)
#
# Uso:
#   REPO="SantiagoRenteria/ideasgroup-fullstack-assessment-santiagorenteria" ./scripts/setup-github-issues.sh
#
# Idempotente: puede correrse más de una vez sin duplicar labels (usa --force)
# ni romperse si un milestone ya existe (el error se ignora y se avisa).

set -euo pipefail

REPO="${REPO:-SantiagoRenteria/ideasgroup-fullstack-assessment-santiagorenteria}"

echo "Repositorio destino: $REPO"
echo

# ---------------------------------------------------------------------------
# 1. Labels (ver docs/METODOLOGIA.md § 5.1)
# ---------------------------------------------------------------------------
echo "== Creando labels =="
gh label create "must-have"    --repo "$REPO" --color "d73a4a" --description "Requisito obligatorio (secciones 6.1-6.9)" --force
gh label create "nice-to-have" --repo "$REPO" --color "a2eeef" --description "Requisito deseable (sección 7)" --force
gh label create "docs"         --repo "$REPO" --color "0075ca" --description "Documentación (README, ADRs)" --force
gh label create "test"         --repo "$REPO" --color "fbca04" --description "Cobertura de pruebas" --force
gh label create "security"     --repo "$REPO" --color "5319e7" --description "OWASP / hardening" --force
gh label create "bug"          --repo "$REPO" --color "e11d21" --description "Defecto sobre algo ya implementado" --force

# ---------------------------------------------------------------------------
# 2. Milestones (una por fase, ver docs/fases-implementacion.md)
# ---------------------------------------------------------------------------
echo
echo "== Creando milestones =="
create_milestone() {
  local title="$1"
  local description="$2"
  gh api "repos/$REPO/milestones" -f title="$title" -f description="$description" >/dev/null 2>&1 \
    && echo "  creado: $title" \
    || echo "  ya existía (o falló, revisar): $title"
}

create_milestone "Fase 0 — Cimientos"                          "Estructura hexagonal, Angular+Sakai, docker-compose esqueleto"
create_milestone "Fase 1 — Autenticación"                      "JWT, hash salt+pepper, guardia de ruta, interceptor"
create_milestone "Fase 2 — Proyectos y Columnas"                "CRUD con paginación, filtro y regla de negocio de columnas"
create_milestone "Fase 3 — Tablero y Ordenamiento"              "Drag&drop, LexoRank, actualización optimista"
create_milestone "Fase 4 — Tiempo real"                         "Canal en tiempo real, grupos por tablero"
create_milestone "Fase 5 — Reportes duales"                     "DTO común, exportadores PDF/Excel extensibles"
create_milestone "Fase 6 — Pruebas, README, ERD, opcionales"    "Cobertura mínima, entregables documentales, deseables"
create_milestone "Fase 7 — Buffer y seguridad"                  "Hardening OWASP, video opcional, margen de commits"

# ---------------------------------------------------------------------------
# 3. Issues
# ---------------------------------------------------------------------------
echo
echo "== Creando issues =="

create_issue() {
  local title="$1"
  local milestone="$2"
  local labels="$3"
  local body="$4"
  gh issue create --repo "$REPO" --title "$title" --milestone "$milestone" --label "$labels" --body "$body"
}

# --- Fase 0 — Cimientos ---
create_issue "[Fase 0] Estructura hexagonal del backend" "Fase 0 — Cimientos" "must-have" \
"**Sección del enunciado:** 6.1

**Criterio de aceptación:**
- [ ] Solución .NET 8 con proyectos Domain / Application / Infrastructure / API separados
- [ ] Domain sin referencias a EF Core ni frameworks externos
- [ ] Solución compila vacía, sin lógica de negocio aún

Ver docs/decisions/arquitectura-decisiones.md § 2."

create_issue "[Fase 0] Proyecto Angular 17 + PrimeNG Sakai operativo" "Fase 0 — Cimientos" "must-have" \
"**Sección del enunciado:** 4, 6.1

**Criterio de aceptación:**
- [ ] Angular 17 con TypeScript y SCSS
- [ ] Plantilla Sakai integrada y arrancando sin errores
- [ ] Configuración externa vía archivos de entorno (sin URLs embebidas en componentes/servicios)"

create_issue "[Fase 0] Docker Compose esqueleto + .env.example" "Fase 0 — Cimientos" "must-have" \
"**Sección del enunciado:** 6.1

**Criterio de aceptación:**
- [ ] docker-compose con PostgreSQL, API, SPA (nginx)
- [ ] .env.example con variables explícitas y valores por defecto que permitan levantar el proyecto sin configuración manual
- [ ] Sin secretos ni connection strings reales versionados"

# --- Fase 1 — Autenticación ---
create_issue "[Fase 1] Modelo de dominio Usuario + seed de 2 usuarios" "Fase 1 — Autenticación" "must-have" \
"**Sección del enunciado:** 5, 6.2

**Criterio de aceptación:**
- [ ] Entidad Usuario (nombre, correo, hash de contraseña)
- [ ] Hash con salt + pepper (BCrypt/Argon2, nunca SHA256 puro)
- [ ] Migración semilla con al menos 2 usuarios precargados"

create_issue "[Fase 1] Emisión y validación de JWT" "Fase 1 — Autenticación" "must-have" \
"**Sección del enunciado:** 6.2

**Criterio de aceptación:**
- [ ] Login emite JWT válido
- [ ] Validación explícita de Issuer, Audience y expiración
- [ ] Todos los endpoints de negocio protegidos con autorización"

create_issue "[Fase 1] Guardia de ruta Angular + interceptor HTTP" "Fase 1 — Autenticación" "must-have" \
"**Sección del enunciado:** 6.2

**Criterio de aceptación:**
- [ ] Guardia impide acceso al tablero sin sesión válida
- [ ] Interceptor adjunta el token en cada petición
- [ ] Respuesta 401 gestionada de forma coherente (ej. redirect a login)"

# --- Fase 2 — Proyectos y Columnas ---
create_issue "[Fase 2] CRUD Proyectos con paginación y filtro" "Fase 2 — Proyectos y Columnas" "must-have" \
"**Sección del enunciado:** 6.3

**Criterio de aceptación:**
- [ ] API RESTful con las 4 operaciones básicas
- [ ] Listado paginado
- [ ] Filtro por nombre con coincidencia parcial resuelto en servidor
- [ ] Alta/edición/eliminación desde la interfaz"

create_issue "[Fase 2] CRUD Columnas + regla de negocio" "Fase 2 — Proyectos y Columnas" "must-have" \
"**Sección del enunciado:** 6.4

**Criterio de aceptación:**
- [ ] API RESTful con las 4 operaciones, incluido orden
- [ ] Regla de negocio en backend: no eliminar columna con tareas asignadas
- [ ] Administración y reordenación desde la interfaz"

# --- Fase 3 — Tablero y Ordenamiento ---
create_issue "[Fase 3] Algoritmo de cálculo de posición (LexoRank) + tests" "Fase 3 — Tablero y Ordenamiento" "must-have,test" \
"**Sección del enunciado:** 6.9 (prueba unitaria exigida explícitamente por nombre)

**Criterio de aceptación:**
- [ ] Implementación TDD, tests antes que UI de drag&drop
- [ ] Test: insertar entre dos posiciones existentes
- [ ] Test: insertar al inicio y al final de la columna
- [ ] Test: caso límite que fuerza rebalanceo (gap agotado)

Ver docs/decisions/arquitectura-decisiones.md § 4."

create_issue "[Fase 3] CRUD Tareas" "Fase 3 — Tablero y Ordenamiento" "must-have" \
"**Sección del enunciado:** 6.5

**Criterio de aceptación:**
- [ ] API RESTful con las 4 operaciones básicas
- [ ] Alta/edición/eliminación desde el tablero
- [ ] Asignación de responsable y prioridad"

create_issue "[Fase 3] Tablero kanban con drag&drop y persistencia de orden" "Fase 3 — Tablero y Ordenamiento" "must-have" \
"**Sección del enunciado:** 6.6

**Criterio de aceptación:**
- [ ] Renderizado dinámico de columnas y tareas en orden
- [ ] Traslado entre columnas por drag&drop
- [ ] Reordenamiento dentro de una columna por drag&drop
- [ ] Orden persiste al recargar y al iniciar sesión desde otro equipo"

create_issue "[Fase 3] Actualización optimista con reversión" "Fase 3 — Tablero y Ordenamiento" "must-have" \
"**Sección del enunciado:** 6.6

**Criterio de aceptación:**
- [ ] UI aplica el movimiento de inmediato (optimistic update)
- [ ] Si el servidor responde error, el movimiento se revierte visiblemente
- [ ] Conflicto de concurrencia (xmin/RowVersion) detectado y expuesto como error"

# --- Fase 4 — Tiempo real ---
create_issue "[Fase 4] Canal de tiempo real autenticado" "Fase 4 — Tiempo real" "must-have" \
"**Sección del enunciado:** 6.2, 6.7

**Criterio de aceptación:**
- [ ] Canal autenticado con el mismo token de sesión
- [ ] Grupos/salas por tablero (proyectoId)
- [ ] Una sesión no recibe eventos de tableros a los que no está suscrita"

create_issue "[Fase 4] Propagación de eventos en tiempo real (<2s)" "Fase 4 — Tiempo real" "must-have" \
"**Sección del enunciado:** 6.7

**Criterio de aceptación:**
- [ ] Alta/edición/eliminación de tareas se propaga a otras sesiones
- [ ] Traslado y nuevo orden se propagan en menos de 2 segundos, sin recarga manual
- [ ] Verificado con dos sesiones simultáneas (idealmente dos usuarios distintos)"

create_issue "[Fase 4] Cierre limpio de conexiones y suscripciones" "Fase 4 — Tiempo real" "must-have" \
"**Sección del enunciado:** 6.7

**Criterio de aceptación:**
- [ ] Conexión y suscripciones se cierran correctamente al destruir el componente
- [ ] Sin conexiones huérfanas verificables"

# --- Fase 5 — Reportes duales ---
create_issue "[Fase 5] DTO común y query única para reporte" "Fase 5 — Reportes duales" "must-have" \
"**Sección del enunciado:** 6.8

**Criterio de aceptación:**
- [ ] Una sola consulta EF (AsNoTracking) alimenta ambos formatos
- [ ] DTO común compartido entre exportadores

Ver docs/decisions/arquitectura-decisiones.md § 5."

create_issue "[Fase 5] Exportador PDF (QuestPDF)" "Fase 5 — Reportes duales" "must-have" \
"**Sección del enunciado:** 6.8

**Criterio de aceptación:**
- [ ] Encabezado con datos del proyecto y fecha de generación
- [ ] Tabla de tareas con columna, responsable y prioridad
- [ ] Extensibilidad: agregar un tercer formato no requiere tocar esta clase"

create_issue "[Fase 5] Exportador Excel" "Fase 5 — Reportes duales" "must-have" \
"**Sección del enunciado:** 6.8

**Criterio de aceptación:**
- [ ] Mismos datos que el PDF
- [ ] Encabezados legibles y anchos de columna adecuados
- [ ] Librería declarada y justificada en el README"

create_issue "[Fase 5] Descarga de reportes desde el frontend" "Fase 5 — Reportes duales" "must-have" \
"**Sección del enunciado:** 6.8

**Criterio de aceptación:**
- [ ] Descarga funcional desde la interfaz
- [ ] Nombre de archivo y Content-Type correctos por formato"

# --- Fase 6 — Pruebas, README, ERD, opcionales ---
create_issue "[Fase 6] Completar cobertura mínima de tests" "Fase 6 — Pruebas, README, ERD, opcionales" "must-have,test" \
"**Sección del enunciado:** 6.9

**Criterio de aceptación:**
- [ ] Al menos 5 pruebas unitarias backend, ejecutan y pasan
- [ ] Al menos 5 pruebas unitarias frontend, ejecutan y pasan
- [ ] Incluye el test de cálculo de posición (ver issue de Fase 3)"

create_issue "[Fase 6] README completo" "Fase 6 — Pruebas, README, ERD, opcionales" "docs" \
"**Sección del enunciado:** 8

**Criterio de aceptación:**
- [ ] Instrucciones de ejecución paso a paso, verificadas en entorno limpio
- [ ] Decisiones arquitectónicas y justificación (link a docs/decisions/)
- [ ] Tecnología de tiempo real elegida y alternativas descartadas
- [ ] Estrategia de índices de ordenamiento
- [ ] Patrón aplicado en la exportación dual
- [ ] Declaración de uso de IA: herramienta y partes del desarrollo donde se usó"

create_issue "[Fase 6] Diagrama ERD embebido" "Fase 6 — Pruebas, README, ERD, opcionales" "docs" \
"**Sección del enunciado:** 8

**Criterio de aceptación:**
- [ ] Imagen (no texto/esquema de herramienta externa) generada desde el esquema real de las migraciones
- [ ] Embebida directamente en el README, visible sin herramientas externas"

create_issue "[Deseable] Filtros por responsable y prioridad" "Fase 6 — Pruebas, README, ERD, opcionales" "nice-to-have" \
"**Sección del enunciado:** 7

**Criterio de aceptación:**
- [ ] Filtro por responsable y por prioridad en el tablero
- [ ] Mismo filtro aplicado al contenido del reporte"

create_issue "[Deseable] Indicador de usuarios conectados al tablero" "Fase 6 — Pruebas, README, ERD, opcionales" "nice-to-have" \
"**Sección del enunciado:** 7

**Criterio de aceptación:**
- [ ] Indicador visible de qué usuarios están conectados al mismo tablero en tiempo real"

create_issue "[Deseable] Búsqueda de tareas por texto" "Fase 6 — Pruebas, README, ERD, opcionales" "nice-to-have" \
"**Sección del enunciado:** 7

**Criterio de aceptación:**
- [ ] Búsqueda de tareas por texto libre dentro del tablero"

# --- Fase 7 — Buffer y seguridad ---
create_issue "[Fase 7] Revisión de seguridad (OWASP)" "Fase 7 — Buffer y seguridad" "security" \
"**Sección del enunciado:** 4

**Criterio de aceptación (ver docs/METODOLOGIA.md § 9.3):**
- [ ] CORS con whitelist explícita
- [ ] Rate limiting en login
- [ ] Validación server-side con FluentValidation en todo input
- [ ] Sin secretos versionados, todo vía variables de entorno"

create_issue "[Fase 7] Video demostrativo (opcional)" "Fase 7 — Buffer y seguridad" "nice-to-have" \
"**Sección del enunciado:** 8 (opcional, +5%)

**Criterio de aceptación:**
- [ ] 5 a 10 minutos, happy path
- [ ] Incluye sincronización en tiempo real con dos sesiones y descarga de ambos reportes
- [ ] Enlace público (Drive/OneDrive/YouTube privado) sin requerir descarga"

echo
echo "== Listo. Revisa el tablero de issues en GitHub. =="
