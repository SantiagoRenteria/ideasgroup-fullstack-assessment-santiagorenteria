using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Infrastructure.Reports;

// Duplica a proposito TASK_PRIORITY_LABELS/PROJECT_STATUS_LABELS del frontend: no hay
// paquete compartido entre Angular y .NET, es consistencia deliberada, no accidental.
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
