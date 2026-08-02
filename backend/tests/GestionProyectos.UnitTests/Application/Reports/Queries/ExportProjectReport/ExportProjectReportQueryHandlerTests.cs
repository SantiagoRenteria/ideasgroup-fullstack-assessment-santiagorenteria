using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Reports;
using GestionProyectos.Application.Reports.Queries.ExportProjectReport;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
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
        _reportRepository.GetReportAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<TaskPriority?>(), Arg.Any<CancellationToken>())
            .Returns((ProjectReportDto?)null);
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter }, NullLogger<ExportProjectReportQueryHandler>.Instance);

        var result = await handler.Handle(new ExportProjectReportQuery(Guid.NewGuid(), "pdf"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportProjectReportQueryHandler.ProjectNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_FormatoNoSoportado_RetornaFailure()
    {
        var projectId = Guid.NewGuid();
        _reportRepository.GetReportAsync(projectId, Arg.Any<Guid?>(), Arg.Any<TaskPriority?>(), Arg.Any<CancellationToken>())
            .Returns(BuildReport(projectId));
        _pdfExporter.Format.Returns("pdf");
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter }, NullLogger<ExportProjectReportQueryHandler>.Instance);

        var result = await handler.Handle(new ExportProjectReportQuery(projectId, "excel"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExportProjectReportQueryHandler.UnsupportedFormat, result.Error);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_FormatoSoportadoCaseInsensitive_RetornaArchivoConFechaDeGeneracionEstampada()
    {
        var projectId = Guid.NewGuid();
        _reportRepository.GetReportAsync(projectId, Arg.Any<Guid?>(), Arg.Any<TaskPriority?>(), Arg.Any<CancellationToken>())
            .Returns(BuildReport(projectId));
        _pdfExporter.Format.Returns("pdf");
        _pdfExporter.ContentType.Returns("application/pdf");
        _pdfExporter.FileExtension.Returns("pdf");
        ProjectReportDto? passedReport = null;
        _pdfExporter.Export(Arg.Do<ProjectReportDto>(r => passedReport = r)).Returns(new byte[] { 1, 2, 3 });
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter }, NullLogger<ExportProjectReportQueryHandler>.Instance);

        var result = await handler.Handle(new ExportProjectReportQuery(projectId, "PDF"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value!.ContentType);
        Assert.StartsWith("reporte-proyecto-demo-", result.Value.FileName);
        Assert.EndsWith(".pdf", result.Value.FileName);

        Assert.NotNull(passedReport);
        Assert.NotEqual(default, passedReport!.GeneratedAt);
    }

    // Deseable seccion 7: el filtro activo en el tablero debe llegar intacto hasta la
    // consulta que arma el reporte -- no alcanza con probarlo en ProjectReportRepository
    // (ver test de runtime contra Postgres), tambien hay que probar que el handler no lo
    // pierde ni lo reemplaza en el camino.
    [Fact]
    public async Task Handle_ConFiltrosDeResponsableYPrioridad_LosPropagaAlRepositorio()
    {
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _reportRepository.GetReportAsync(projectId, assigneeId, TaskPriority.Urgent, Arg.Any<CancellationToken>())
            .Returns(BuildReport(projectId));
        _pdfExporter.Format.Returns("pdf");
        _pdfExporter.Export(Arg.Any<ProjectReportDto>()).Returns(new byte[] { 1 });
        var handler = new ExportProjectReportQueryHandler(_reportRepository, new[] { _pdfExporter }, NullLogger<ExportProjectReportQueryHandler>.Instance);

        var result = await handler.Handle(new ExportProjectReportQuery(projectId, "pdf", assigneeId, TaskPriority.Urgent), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _reportRepository.Received(1).GetReportAsync(projectId, assigneeId, TaskPriority.Urgent, Arg.Any<CancellationToken>());
    }
}
