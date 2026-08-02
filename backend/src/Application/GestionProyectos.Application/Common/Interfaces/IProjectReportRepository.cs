using GestionProyectos.Application.Reports;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IProjectReportRepository
{
    // Una sola consulta EF, AsNoTracking (enunciado sección 6.8 y
    // docs/decisions/arquitectura-decisiones.md §5). Devuelve null solo si el proyecto no
    // existe -- un proyecto sin columnas o sin tareas sí devuelve DTO, con Tasks vacío.
    Task<ProjectReportDto?> GetReportAsync(Guid projectId, CancellationToken cancellationToken);
}
