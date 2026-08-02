using GestionProyectos.Application.Reports;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IProjectReportRepository
{
    // Una sola consulta EF, AsNoTracking (enunciado sección 6.8 y
    // docs/decisions/arquitectura-decisiones.md §5). Devuelve null solo si el proyecto no
    // existe -- un proyecto sin columnas o sin tareas sí devuelve DTO, con Tasks vacío.
    // assigneeId/priority (deseable sección 7) filtran las tareas incluidas, pero nunca
    // hacen desaparecer la fila del proyecto: un proyecto existente sin tareas que
    // matcheen el filtro sigue devolviendo DTO con Tasks vacío, no null.
    Task<ProjectReportDto?> GetReportAsync(
        Guid projectId,
        Guid? assigneeId,
        TaskPriority? priority,
        CancellationToken cancellationToken);
}
