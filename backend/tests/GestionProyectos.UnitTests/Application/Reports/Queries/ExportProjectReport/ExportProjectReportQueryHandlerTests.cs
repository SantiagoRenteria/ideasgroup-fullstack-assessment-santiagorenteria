using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Reports;
using GestionProyectos.Application.Reports.Queries.ExportProjectReport;
using GestionProyectos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace GestionProyectos.UnitTests.Application.Reports.Queries.ExportProjectReport;

public class ExportProjectReportQueryHandlerTests
{
    private readonly IProjectReportRepository _reportRepository = Substitute.For<IProjectReportRepository>();
    private readonly IReportExporter _pdfExporter = Substitute.For<IReportExporter>();

    private static ProjectReportDto BuildReport(Guid projectId) => new(
        projectId, "Proyecto Demo", "Descripcion",
        new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatus.InProgress,
        default,
        new List<TaskReportItemDto> { new("Por hacer", "Tarea", "Detalle", TaskPriority.High, "Ana") });

    [Fact]
    public async Task Handle_ProyectoNoExiste_RetornaFailure()
    {
        _reportRepository.GetReportAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProjectReportDto?)null);
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter });

        var result = await handler.Handle(new ExportProjectReportQuery(Guid.NewGuid(), "pdf"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportProjectReportQueryHandler.ProjectNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_FormatoNoSoportado_RetornaFailure()
    {
        var projectId = Guid.NewGuid();
        _reportRepository.GetReportAsync(projectId, Arg.Any<CancellationToken>()).Returns(BuildReport(projectId));
        _pdfExporter.Format.Returns("pdf");
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter });

        var result = await handler.Handle(new ExportProjectReportQuery(projectId, "excel"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportProjectReportQueryHandler.UnsupportedFormat, result.Error);
    }

    [Fact]
    public async Task Handle_FormatoSoportadoCaseInsensitive_RetornaArchivoConFechaDeGeneracionEstampada()
    {
        var projectId = Guid.NewGuid();
        _reportRepository.GetReportAsync(projectId, Arg.Any<CancellationToken>()).Returns(BuildReport(projectId));
        _pdfExporter.Format.Returns("pdf");
        _pdfExporter.ContentType.Returns("application/pdf");
        _pdfExporter.FileExtension.Returns("pdf");
        ProjectReportDto? passedReport = null;
        _pdfExporter.Export(Arg.Do<ProjectReportDto>(r => passedReport = r)).Returns(new byte[] { 1, 2, 3 });
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter });

        var result = await handler.Handle(new ExportProjectReportQuery(projectId, "PDF"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value!.ContentType);
        Assert.StartsWith("reporte-proyecto-demo-", result.Value.FileName);
        Assert.EndsWith(".pdf", result.Value.FileName);

        Assert.NotNull(passedReport);
        Assert.NotEqual(default, passedReport!.GeneratedAt);
    }
}
