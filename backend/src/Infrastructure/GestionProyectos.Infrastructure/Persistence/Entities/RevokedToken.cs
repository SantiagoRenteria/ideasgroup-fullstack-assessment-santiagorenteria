namespace GestionProyectos.Infrastructure.Persistence.Entities;

// Registro tecnico de seguridad (blocklist de JWT), no una entidad de Domain: no tiene
// invariantes de negocio ni participa de ningun caso de uso -- ver
// docs/decisions/arquitectura-decisiones.md §16.
public class RevokedToken
{
    public string Jti { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }

    private RevokedToken() { }

    public RevokedToken(string jti, DateTime expiresAtUtc)
    {
        Jti = jti;
        ExpiresAtUtc = expiresAtUtc;
    }
}
