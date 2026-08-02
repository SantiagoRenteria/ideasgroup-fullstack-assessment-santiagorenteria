using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Projects;

public static class ProjectMappingExtensions
{
    public static ProjectResponseDto ToDto(this Project project) =>
        new(project.Id, project.Name, project.Description, project.DateRange.Start, project.DateRange.End, project.Status);
}
