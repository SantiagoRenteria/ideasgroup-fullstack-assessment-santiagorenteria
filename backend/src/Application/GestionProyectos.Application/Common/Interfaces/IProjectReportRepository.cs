using GestionProyectos.Application.Reports;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IProjectReportRepository
{
    // Una sola consulta EF, AsNoTracking (sección 6.8, ADR §5): null solo si el proyecto
    // no existe; sin tareas (o filtradas) igual devuelve DTO con Tasks vacío.
    Task<ProjectReportDto?> GetReportAsync(
        Guid projectId,
        Guid? assigneeId,
        TaskPriority? priority,
        CancellationToken cancellationToken);
}
