using GestionProyectos.Domain.Common;

namespace GestionProyectos.Domain.ValueObjects;

// Value Object: antes TaskEntity.Order solo validaba "no vacio" -- cualquier string pasaba
// como clave LexoRank "valida". Ahora valida tambien el alfabeto (base62, LexoRankService)
// y encapsula la comparacion ordinal, en vez de que cada consumidor recuerde usar
// StringComparer.Ordinal (ver arquitectura-decisiones.md §22).
public sealed record LexoRankKey : IComparable<LexoRankKey>
{
    public string Value { get; }

    public LexoRankKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El orden de la tarea es obligatorio.", nameof(value));

        if (value.Any(c => !LexoRankService.IsValidCharacter(c)))
            throw new ArgumentException("El orden de la tarea contiene caracteres fuera del alfabeto LexoRank.", nameof(value));

        Value = value;
    }

    public int CompareTo(LexoRankKey? other) => other is null ? 1 : string.CompareOrdinal(Value, other.Value);

    public static bool operator <(LexoRankKey left, LexoRankKey right) => left.CompareTo(right) < 0;
    public static bool operator >(LexoRankKey left, LexoRankKey right) => left.CompareTo(right) > 0;
    public static bool operator <=(LexoRankKey left, LexoRankKey right) => left.CompareTo(right) <= 0;
    public static bool operator >=(LexoRankKey left, LexoRankKey right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value;
}
