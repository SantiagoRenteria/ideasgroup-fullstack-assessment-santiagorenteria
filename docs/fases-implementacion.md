# Plan de Fases — Prueba Técnica IDEASGROUP-REM-LAT-26-2907

La ponderación es clara: 60% funcionalidades obligatorias, 40% sustentación, +5% video, +5% opcionales. Es decir, lo obligatorio bien sustentado pesa más que lo opcional sin poder explicarlo. Con 7 días calendario, la estructura es:

## Fase 0 — Cimientos (día 1)

Arquitectura hexagonal del backend (carpetas Domain/Application/Infrastructure vacías pero correctas), proyecto Angular con Sakai operativo, docker-compose esqueleto, `.env.example`. No suma puntos directos, pero todo lo demás depende de que esté bien desde el inicio — cambiar de arquitectura a mitad de camino es carísimo.

## Fase 1 — Autenticación y modelo de dominio (día 1-2)

JWT, hash con salt+pepper, seed de 2 usuarios, guardia de ruta, interceptor con manejo de 401. Es el prerequisito de todo lo demás (todos los endpoints de negocio van protegidos), así que va primero, no al final.

## Fase 2 — CRUD de Proyectos y Columnas (día 2-3)

Con paginación/filtro en servidor y la regla de negocio de "no borrar columna con tareas". Es la base de datos relacional completa antes de tocar el tablero.

## Fase 3 — Tablero kanban + tareas + drag&drop + persistencia de orden (día 3-4)

Núcleo técnico más pesado: aquí se juega la prueba unitaria obligatoria del cálculo de posición. Se escribe antes de la UI de drag&drop, como TDD, porque es el único test que el enunciado exige explícitamente por nombre (sección 6.9).

## Fase 4 — Tiempo real (día 4-5)

Sobre una base de CRUD ya sólida y probada, para no depurar dos problemas a la vez (¿falla la lógica o falla el socket?). Grupos por tablero, cierre limpio de conexiones, actualización optimista con reversión.

## Fase 5 — Reportes duales (día 5)

DTO común, QuestPDF + Excel, patrón de exportación extensible. Va después porque depende de que el modelo de datos ya esté estable (columna, responsable, prioridad).

## Fase 6 — Pruebas restantes, README, diagrama, opcionales (día 6)

El README no es un trámite — pide explícitamente justificar decisiones y alternativas descartadas, que es material directo para la sustentación. Se escribe en paralelo desde el día 1, no al final.

## Fase 7 — Buffer, commits atómicos, video opcional (día 7)

Margen para que los commits luzcan distribuidos de forma honesta (no forzada al final), tal como exige la sección 9 del enunciado.

---

Mapeo de fases a ramas e issues: ver `docs/METODOLOGIA.md` § Flujo Git y § Gestión de Issues.
