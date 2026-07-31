namespace GestionProyectos.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Correo { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    private Usuario() { }

    public Usuario(Guid id, string nombre, string correo, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del usuario es obligatorio.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(correo))
            throw new ArgumentException("El correo del usuario es obligatorio.", nameof(correo));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        Id = id;
        Nombre = nombre;
        Correo = correo.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }
}
