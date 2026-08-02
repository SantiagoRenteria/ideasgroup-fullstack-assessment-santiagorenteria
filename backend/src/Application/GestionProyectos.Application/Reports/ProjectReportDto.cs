using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Reports;

// DTO compartido por todos los IReportExporter (ADR §5): ningun exportador conoce EF ni
// el dominio. GeneratedAt lo estampa el handler, no el repositorio.
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
