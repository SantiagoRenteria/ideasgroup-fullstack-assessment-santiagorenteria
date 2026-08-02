using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Reports.Queries.ExportProjectReport;

public record ExportProjectReportQuery(Guid ProjectId, string Format) : IQuery<Result<ExportedReportDto>>;
