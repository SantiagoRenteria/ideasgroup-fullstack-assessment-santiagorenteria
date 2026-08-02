namespace GestionProyectos.Application.Reports;

public record ExportedReportDto(byte[] Content, string ContentType, string FileName);
