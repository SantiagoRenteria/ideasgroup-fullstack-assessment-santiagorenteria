using ClosedXML.Excel;
using GestionProyectos.Application.Reports;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Reports;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure.Reports;

public class ClosedXmlReportExporterTests
{
    private readonly ClosedXmlReportExporter _exporter = new();

    private static ProjectReportDto BuildReport(IReadOnlyList<TaskReportItemDto> tasks) => new(
        Guid.NewGuid(), "Proyecto Demo", "Descripcion del proyecto",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.InProgress,
        new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), tasks);

    [Fact]
    public void Export_ExponeMetadatosDeFormatoExcel()
    {
        Assert.Equal("excel", _exporter.Format);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _exporter.ContentType);
        Assert.Equal("xlsx", _exporter.FileExtension);
    }

    [Fact]
    public void Export_ProyectoConTareas_GeneraWorkbookConLosMismosDatosQueElPdf()
    {
        var report = BuildReport(new List<TaskReportItemDto>
        {
            new("Por hacer", "Diseñar wireframes", "Bocetos iniciales", TaskPriority.High, "Ana"),
            new("Hecho", "Cimientos", "Arquitectura hexagonal", TaskPriority.Low, null)
        });

        var bytes = _exporter.Export(report);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Proyecto Demo", sheet.Cell(1, 1).GetString());

        Assert.Equal("Tarea", sheet.Cell(6, 1).GetString());
        Assert.Equal("Columna", sheet.Cell(6, 2).GetString());
        Assert.Equal("Responsable", sheet.Cell(6, 3).GetString());
        Assert.Equal("Prioridad", sheet.Cell(6, 4).GetString());

        Assert.Equal("Diseñar wireframes", sheet.Cell(7, 1).GetString());
        Assert.Equal("Por hacer", sheet.Cell(7, 2).GetString());
        Assert.Equal("Ana", sheet.Cell(7, 3).GetString());
        Assert.Equal("Alta", sheet.Cell(7, 4).GetString());

        Assert.Equal("Cimientos", sheet.Cell(8, 1).GetString());
        Assert.Equal("Sin asignar", sheet.Cell(8, 3).GetString());
        Assert.Equal("Baja", sheet.Cell(8, 4).GetString());
    }

    [Fact]
    public void Export_ProyectoSinTareas_NoLanzaYNoDejaFilasVaciasEngañosas()
    {
        var report = BuildReport(Array.Empty<TaskReportItemDto>());

        var bytes = _exporter.Export(report);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Este proyecto no tiene tareas registradas.", sheet.Cell(7, 1).GetString());
    }
}
