using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Infrastructure.Reports;

// Traduccion compartida por todos los IReportExporter (PDF, Excel...). Duplica a proposito
// los mismos textos que TASK_PRIORITY_LABELS / PROJECT_STATUS_LABELS del frontend (ver
// frontend/src/app/features/board/models/task.model.ts y
// frontend/src/app/features/projects/models/project.model.ts): no hay paquete compartido
// entre Angular y .NET, asi que mantener la misma redaccion en ambos lados es una decision
// deliberada de consistencia, no una duplicacion accidental.
internal static class ReportLabels
{
    public static string Priority(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "Baja",
        TaskPriority.Medium => "Media",
        TaskPriority.High => "Alta",
        TaskPriority.Urgent => "Urgente",
        _ => priority.ToString()
    };

    public static string Status(ProjectStatus status) => status switch
    {
        ProjectStatus.Planned => "Planificado",
        ProjectStatus.InProgress => "En progreso",
        ProjectStatus.Completed => "Completado",
        ProjectStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };
}
