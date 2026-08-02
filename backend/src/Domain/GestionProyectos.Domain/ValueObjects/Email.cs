using System.Text.RegularExpressions;

namespace GestionProyectos.Domain.ValueObjects;

// Value Object: normaliza (trim + lowercase) y valida formato una sola vez, en vez de
// repetir la logica en cada punto que recibe un correo (ver arquitectura-decisiones.md §22).
public sealed partial record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El correo es obligatorio.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailFormatRegex().IsMatch(normalized))
            throw new ArgumentException("El formato de correo no es valido.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormatRegex();
}
