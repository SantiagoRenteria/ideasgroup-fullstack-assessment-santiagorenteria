using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Reports.Queries.ExportProjectReport;

// Handler = unico orquestador (ver docs/decisions/arquitectura-decisiones.md §5): pide el
// DTO por la unica consulta EF (IProjectReportRepository), resuelve el exportador por
// formato via IEnumerable<IReportExporter> (nunca if/switch) y estampa la fecha de
// generacion -- no es dato persistido, no le corresponde al repositorio.
public class ExportProjectReportQueryHandler : IRequestHandler<ExportProjectReportQuery, Result<ExportedReportDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";
    public const string UnsupportedFormat = "Formato de reporte no soportado.";

    private readonly IProjectReportRepository _reportRepository;
    private readonly IEnumerable<IReportExporter> _exporters;

    public ExportProjectReportQueryHandler(IProjectReportRepository reportRepository, IEnumerable<IReportExporter> exporters)
    {
        _reportRepository = reportRepository;
        _exporters = exporters;
    }

    public async Task<Result<ExportedReportDto>> Handle(ExportProjectReportQuery request, CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetReportAsync(request.ProjectId, cancellationToken);

        if (report is null)
            return Result<ExportedReportDto>.Failure(ProjectNotFound);

        var exporter = _exporters.FirstOrDefault(e => e.Format.Equals(request.Format, StringComparison.OrdinalIgnoreCase));

        if (exporter is null)
            return Result<ExportedReportDto>.Failure(UnsupportedFormat);

        var generatedReport = report with { GeneratedAt = DateTime.UtcNow };
        var content = exporter.Export(generatedReport);
        var fileName = $"reporte-{Slugify(generatedReport.ProjectName)}-{DateOnly.FromDateTime(generatedReport.GeneratedAt):yyyy-MM-dd}.{exporter.FileExtension}";

        return Result<ExportedReportDto>.Success(new ExportedReportDto(content, exporter.ContentType, fileName));
    }

    private static string Slugify(string value) =>
        new string(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
}
