using System.Security.Cryptography;
using System.Text;
using GestionProyectos.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace GestionProyectos.Infrastructure.Security;

// Salt: generado y embebido por BCrypt en cada hash. Pepper: secreto de aplicacion
// (fuera de la base de datos) mezclado via HMACSHA256 antes de aplicar BCrypt, para
// que una fuga de la base de datos por si sola no alcance para crackear los hashes.
public class BCryptPasswordHasher : IPasswordHasher
{
    private readonly byte[] _pepperKey;

    public BCryptPasswordHasher(IOptions<SecurityOptions> options)
    {
        _pepperKey = Encoding.UTF8.GetBytes(options.Value.Pepper);
    }

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(ApplyPepper(password));

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(ApplyPepper(password), passwordHash);

    private string ApplyPepper(string password)
    {
        using var hmac = new HMACSHA256(_pepperKey);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
