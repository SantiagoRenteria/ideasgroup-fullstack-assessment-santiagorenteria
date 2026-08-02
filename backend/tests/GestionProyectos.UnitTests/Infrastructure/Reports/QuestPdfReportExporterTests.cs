using System.Text;
using GestionProyectos.Application.Reports;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Reports;
using QuestPDF.Infrastructure;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure.Reports;

public class QuestPdfReportExporterTests
{
    // QuestPDF exige declarar la licencia antes de generar cualquier documento (ver
    // DependencyInjection.cs) -- este test corre fuera del host de la API, asi que la
    // declara por su cuenta.
    static QuestPdfReportExporterTests() => QuestPDF.Settings.License = LicenseType.Community;

    private readonly QuestPdfReportExporter _exporter = new();

    private static ProjectReportDto BuildReport(IReadOnlyList<TaskReportItemDto> tasks) => new(
        Guid.NewGuid(), "Proyecto Demo", "Descripcion del proyecto",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.InProgress,
        new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), tasks);

    [Fact]
    public void Export_ExponeMetadatosDeFormatoPdf()
    {
        Assert.Equal("pdf", _exporter.Format);
        Assert.Equal("application/pdf", _exporter.ContentType);
        Assert.Equal("pdf", _exporter.FileExtension);
    }

    [Fact]
    public void Export_ProyectoConTareas_GeneraPdfValido()
    {
        var report = BuildReport(new List<TaskReportItemDto>
        {
            new("Por hacer", "Diseñar wireframes", "Bocetos iniciales", TaskPriority.High, "Ana"),
            new("Hecho", "Cimientos", "Arquitectura hexagonal", TaskPriority.Low, null)
        });

        var bytes = _exporter.Export(report);

        Assert.NotEmpty(bytes);
        // Firma binaria estandar de un archivo PDF (enunciado 6.8: "reporte PDF" real, no
        // un archivo de texto disfrazado).
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Export_ProyectoSinTareas_NoLanzaYGeneraPdfValido()
    {
        var report = BuildReport(Array.Empty<TaskReportItemDto>());

        var bytes = _exporter.Export(report);

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
