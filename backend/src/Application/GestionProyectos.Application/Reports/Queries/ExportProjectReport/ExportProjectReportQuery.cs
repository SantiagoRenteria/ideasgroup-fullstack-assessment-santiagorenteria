using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Reports.Queries.ExportProjectReport;

// AssigneeId/Priority opcionales (deseable seccion 7: "mismo filtro aplicado al contenido
// del reporte") -- el mismo filtro que el usuario tiene activo en el tablero se manda al
// exportar, para que el PDF/Excel refleje exactamente lo que esta viendo.
public record ExportProjectReportQuery(
    Guid ProjectId,
    string Format,
    Guid? AssigneeId = null,
    TaskPriority? Priority = null) : IQuery<Result<ExportedReportDto>>;
