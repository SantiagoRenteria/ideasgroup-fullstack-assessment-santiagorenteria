namespace GestionProyectos.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    private User() { }

    public User(Guid id, string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del usuario es obligatorio.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El correo del usuario es obligatorio.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        Id = id;
        Name = name;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }
}
