namespace GestionProyectos.Application.Reports;

// Puerto resuelto por IEnumerable<IReportExporter> (ver ExportProjectReportQueryHandler),
// nunca por if/switch de formato: agregar un tercer formato es una clase nueva + un
// registro DI, sin tocar el handler ni las clases exportadoras existentes (requisito de
// extensibilidad, enunciado sección 6.8).
public interface IReportExporter
{
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Export(ProjectReportDto report);
}
