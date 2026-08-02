using ClosedXML.Excel;
using GestionProyectos.Application.Reports;

namespace GestionProyectos.Infrastructure.Reports;

// Mismos datos que QuestPdfReportExporter (enunciado seccion 6.8: "Reporte Excel con los
// mismos datos"), pero cada exportador decide su propio layout -- ninguno conoce al otro,
// solo comparten ProjectReportDto.
public class ClosedXmlReportExporter : IReportExporter
{
    public string Format => "excel";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    public byte[] Export(ProjectReportDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Reporte");

        sheet.Cell(1, 1).Value = report.ProjectName;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 16;

        sheet.Cell(2, 1).Value = report.ProjectDescription;

        sheet.Cell(3, 1).Value = "Inicio:";
        sheet.Cell(3, 2).Value = report.StartDate.ToDateTime(TimeOnly.MinValue);
        sheet.Cell(3, 2).Style.NumberFormat.Format = "dd/mm/yyyy";
        sheet.Cell(3, 3).Value = "Fin previsto:";
        sheet.Cell(3, 4).Value = report.EndDate.ToDateTime(TimeOnly.MinValue);
        sheet.Cell(3, 4).Style.NumberFormat.Format = "dd/mm/yyyy";
        sheet.Cell(3, 5).Value = "Estado:";
        sheet.Cell(3, 6).Value = ReportLabels.Status(report.Status);

        sheet.Cell(4, 1).Value = $"Generado el {report.GeneratedAt:dd/MM/yyyy HH:mm} UTC";
        sheet.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
        sheet.Cell(4, 1).Style.Font.FontSize = 9;

        const int headerRow = 6;
        var headers = new[] { "Tarea", "Columna", "Responsable", "Prioridad" };

        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F0F0");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var row = headerRow + 1;

        foreach (var task in report.Tasks)
        {
            sheet.Cell(row, 1).Value = task.Title;
            sheet.Cell(row, 2).Value = task.ColumnName;
            sheet.Cell(row, 3).Value = task.AssigneeName ?? "Sin asignar";
            sheet.Cell(row, 4).Value = ReportLabels.Priority(task.Priority);
            row++;
        }

        if (report.Tasks.Count == 0)
        {
            sheet.Range(row, 1, row, headers.Length).Merge();
            sheet.Cell(row, 1).Value = "Este proyecto no tiene tareas registradas.";
            sheet.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
        }

        // AdjustToContents() por si el texto es largo, con un piso razonable para que las
        // columnas nunca queden ilegibles con datos cortos (enunciado 6.8: "anchos de
        // columna adecuados").
        sheet.Columns(1, headers.Length).AdjustToContents();
        if (sheet.Column(1).Width < 25) sheet.Column(1).Width = 25;
        if (sheet.Column(3).Width < 18) sheet.Column(3).Width = 18;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
