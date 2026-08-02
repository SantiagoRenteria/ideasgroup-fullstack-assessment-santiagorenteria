namespace GestionProyectos.Infrastructure.Security;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public string AllowedOrigin { get; set; } = string.Empty;
}
