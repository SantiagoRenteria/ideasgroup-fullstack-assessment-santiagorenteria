namespace GestionProyectos.Infrastructure.Security;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public string Pepper { get; set; } = string.Empty;
}
