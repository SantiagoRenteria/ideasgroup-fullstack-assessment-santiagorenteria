using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Reports.Queries.ExportProjectReport;

// Handler = unico orquestador (ADR §5): arma el DTO, resuelve el exportador via
// IEnumerable<IReportExporter> (nunca if/switch) y estampa la fecha de generacion.
public class ExportProjectReportQueryHandler : IRequestHandler<ExportProjectReportQuery, Result<ExportedReportDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";
    public const string UnsupportedFormat = "Formato de reporte no soportado.";

    private readonly IProjectReportRepository _reportRepository;
    private readonly IEnumerable<IReportExporter> _exporters;
    private readonly ILogger<ExportProjectReportQueryHandler> _logger;

    public ExportProjectReportQueryHandler(
        IProjectReportRepository reportRepository,
        IEnumerable<IReportExporter> exporters,
        ILogger<ExportProjectReportQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _exporters = exporters;
        _logger = logger;
    }

    public async Task<Result<ExportedReportDto>> Handle(ExportProjectReportQuery request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetReportAsync(request.ProjectId, request.AssigneeId, request.Priority, cancellationToken);

        if (report is null)
        {
            _logger.LogWarning("Intento de exportar el reporte del proyecto inexistente {ProjectId}", request.ProjectId);
            return Result<ExportedReportDto>.Failure(ProjectNotFound, ErrorType.NotFound);
        }

        var exporter = _exporters.FirstOrDefault(e => e.Format.Equals(request.Format, StringComparison.OrdinalIgnoreCase));

        if (exporter is null)
        {
            _logger.LogWarning("Intento de exportar el reporte del proyecto {ProjectId} en formato no soportado {Format}", request.ProjectId, request.Format);
            return Result<ExportedReportDto>.Failure(UnsupportedFormat, ErrorType.Validation);
        }

        var generatedReport = report with { GeneratedAt = DateTime.UtcNow };
        var content = exporter.Export(generatedReport);
        var fileName = $"reporte-{Slugify(generatedReport.ProjectName)}-{DateOnly.FromDateTime(generatedReport.GeneratedAt):yyyy-MM-dd}.{exporter.FileExtension}";

        return Result<ExportedReportDto>.Success(new ExportedReportDto(content, exporter.ContentType, fileName));
    }

    private static string Slugify(string value) =>
        new string(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
}
