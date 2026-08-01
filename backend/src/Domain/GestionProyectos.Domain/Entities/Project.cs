namespace GestionProyectos.Domain.Entities;

public class Project
{
    private readonly List<Column> _columns = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public ProjectStatus Status { get; private set; }
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    private Project() { }

    public Project(
        Guid id,
        string name,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        ProjectStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del proyecto es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion del proyecto es obligatoria.", nameof(description));

        if (endDate < startDate)
            throw new ArgumentException("La fecha de fin prevista no puede ser anterior a la fecha de inicio.", nameof(endDate));

        Id = id;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public void Update(string name, string description, DateOnly startDate, DateOnly endDate, ProjectStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del proyecto es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripcion del proyecto es obligatoria.", nameof(description));

        if (endDate < startDate)
            throw new ArgumentException("La fecha de fin prevista no puede ser anterior a la fecha de inicio.", nameof(endDate));

        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }
}
