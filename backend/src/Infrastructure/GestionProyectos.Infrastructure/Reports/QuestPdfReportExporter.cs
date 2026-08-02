using GestionProyectos.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionProyectos.Infrastructure.Reports;

// Requisito de extensibilidad (enunciado seccion 6.8): esta clase no conoce Excel ni
// ningun otro formato, y agregar un tercer exportador no la toca -- solo implementa
// IReportExporter y se registra en DI (ver DependencyInjection.cs).
public class QuestPdfReportExporter : IReportExporter
{
    public string Format => "pdf";
    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    public byte[] Export(ProjectReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(report.ProjectName).FontSize(18).Bold();
                    column.Item().PaddingTop(2).Text(report.ProjectDescription).FontColor(Colors.Grey.Darken2);

                    column.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Inicio: {report.StartDate:dd/MM/yyyy}");
                        row.RelativeItem().Text($"Fin previsto: {report.EndDate:dd/MM/yyyy}");
                        row.RelativeItem().Text($"Estado: {ReportLabels.Status(report.Status)}");
                    });

                    column.Item().PaddingTop(4)
                        .Text($"Generado el {report.GeneratedAt:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(15).Element(content => ComposeTasksTable(content, report));

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeTasksTable(QuestPDF.Infrastructure.IContainer content, ProjectReportDto report)
    {
        if (report.Tasks.Count == 0)
        {
            content.Text("Este proyecto no tiene tareas registradas.").FontColor(Colors.Grey.Darken1);
            return;
        }

        content.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Tarea");
                header.Cell().Element(HeaderCell).Text("Columna");
                header.Cell().Element(HeaderCell).Text("Responsable");
                header.Cell().Element(HeaderCell).Text("Prioridad");
            });

            foreach (var task in report.Tasks)
            {
                table.Cell().Element(BodyCell).Text(task.Title);
                table.Cell().Element(BodyCell).Text(task.ColumnName);
                table.Cell().Element(BodyCell).Text(task.AssigneeName ?? "Sin asignar");
                table.Cell().Element(BodyCell).Text(ReportLabels.Priority(task.Priority));
            }
        });
    }

    private static QuestPDF.Infrastructure.IContainer HeaderCell(QuestPDF.Infrastructure.IContainer container) =>
        container.DefaultTextStyle(x => x.Bold()).Padding(5).Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

    private static QuestPDF.Infrastructure.IContainer BodyCell(QuestPDF.Infrastructure.IContainer container) =>
        container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
}
