using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Reports;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Reports;

public class ProjectReportRepository : IProjectReportRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Una sola consulta EF (enunciado sección 6.8): LEFT JOIN encadenado Project -> Columns
    // -> Tasks -> User, arrancando desde Projects (no desde Tasks) a propósito. Así, un
    // proyecto sin columnas o sin tareas sigue devolviendo exactamente una fila (con los
    // campos de tarea en null) en vez de cero filas -- necesario para distinguir "proyecto
    // no existe" (0 filas) de "proyecto existe pero sin tareas" (>=1 fila, Tasks vacío tras
    // filtrar). Los HasQueryFilter de soft-delete (Project/Column/TaskEntity) se aplican
    // solos, sin repetir el filtro aquí.
    public async Task<ProjectReportDto?> GetReportAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await (
            from p in _dbContext.Projects.AsNoTracking()
            where p.Id == projectId
            join c in _dbContext.Columns.AsNoTracking() on p.Id equals c.ProjectId into columnsJoin
            from c in columnsJoin.DefaultIfEmpty()
            join t in _dbContext.Tasks.AsNoTracking() on c.Id equals t.ColumnId into tasksJoin
            from t in tasksJoin.DefaultIfEmpty()
            join u in _dbContext.Users.AsNoTracking() on t.AssigneeId equals u.Id into assigneeJoin
            from u in assigneeJoin.DefaultIfEmpty()
            orderby c.Order, t.Order
            select new
            {
                p.Id,
                p.Name,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.Status,
                ColumnName = c.Name,
                TaskId = (Guid?)t.Id,
                TaskTitle = t.Title,
                TaskDescription = t.Description,
                TaskPriority = (TaskPriority?)t.Priority,
                AssigneeName = u.Name
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return null;

        var header = rows[0];

        var tasks = rows
            .Where(r => r.TaskId is not null)
            .Select(r => new TaskReportItemDto(r.ColumnName!, r.TaskTitle!, r.TaskDescription!, r.TaskPriority!.Value, r.AssigneeName))
            .ToList();

        return new ProjectReportDto(
            header.Id, header.Name, header.Description, header.StartDate, header.EndDate, header.Status,
            default, tasks);
    }
}
