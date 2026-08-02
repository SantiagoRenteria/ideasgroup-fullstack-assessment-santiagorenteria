namespace GestionProyectos.Application.Reports;

// Puerto resuelto por IEnumerable<IReportExporter>, nunca por if/switch: un formato
// nuevo es una clase + un registro DI (extensibilidad, sección 6.8).
public interface IReportExporter
{
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Export(ProjectReportDto report);
}
