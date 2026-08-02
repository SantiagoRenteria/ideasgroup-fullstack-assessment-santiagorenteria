using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Reports;

// DTO compartido por todos los IReportExporter (ver docs/decisions/arquitectura-decisiones.md
// §5): ni QuestPdfReportExporter ni ClosedXmlReportExporter conocen EF ni el dominio, solo
// este contrato. GeneratedAt lo estampa el handler (no es dato persistido, ver
// ExportProjectReportQueryHandler), no el repositorio.
public record ProjectReportDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    DateTime GeneratedAt,
    IReadOnlyList<TaskReportItemDto> Tasks);

public record TaskReportItemDto(
    string ColumnName,
    string Title,
    string Description,
    TaskPriority Priority,
    string? AssigneeName);
