namespace GestionProyectos.Domain.Common;

// LexoRank simplificado (docs/decisions/arquitectura-decisiones.md §4): genera claves
// ordenables lexicograficamente entre dos posiciones existentes, sin reescribir el resto
// de la columna en cada movimiento. Alfabeto base62 ascendente segun el orden ASCII real
// de sus caracteres (0-9 < A-Z < a-z), asi que la comparacion de strings comun ya respeta
// el orden del alfabeto sin tabla de traduccion aparte.
public static class LexoRankService
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Base = 62;

    // Umbral que fuerza un rebalanceo de la columna en vez de dejar crecer la clave sin
    // limite. Insertar siempre en el mismo extremo agota el hueco disponible y hace que
    // cada insercion sucesiva necesite un caracter mas -- este limite corta ese
    // crecimiento antes de que las claves se vuelvan indices imposibles de leer o de
    // indexar eficientemente en Postgres.
    private const int MaxKeyLength = 8;

    public static string GetKeyBetween(string? prev, string? next)
    {
        if (prev is not null && next is not null && string.CompareOrdinal(prev, next) >= 0)
            throw new ArgumentException("La clave anterior debe ser estrictamente menor que la siguiente.");

        var key = Build(prev, next);

        if (key.Length > MaxKeyLength)
            throw new LexoRankRebalanceRequiredException();

        return key;
    }

    // Genera `count` claves cortas y estrictamente ascendentes, reutilizando el mismo
    // algoritmo de punto medio de forma recursiva (biseccion) en vez de una formula de
    // reparto aparte -- un solo algoritmo cubre tanto la insercion normal como el
    // rebalanceo completo de una columna.
    public static IReadOnlyList<string> GenerateSequence(int count)
    {
        if (count <= 0)
            return Array.Empty<string>();

        var keys = new string[count];
        Fill(keys, 0, count - 1, null, null);
        return keys;
    }

    private static void Fill(string[] keys, int lo, int hi, string? lowerBound, string? upperBound)
    {
        if (lo > hi)
            return;

        var mid = lo + (hi - lo) / 2;
        var key = Build(lowerBound, upperBound);
        keys[mid] = key;

        Fill(keys, lo, mid - 1, lowerBound, key);
        Fill(keys, mid + 1, hi, key, upperBound);
    }

    // Construye caracter por caracter el punto medio entre `prev` y `next`. En cada
    // posicion, si hay hueco (diferencia de indices > 1) se resuelve con un unico
    // caracter intermedio; si las claves son adyacentes en esa posicion, se conserva el
    // digito de `prev` (o el minimo si `prev` ya se agoto) y se profundiza una posicion
    // mas -- ahi es donde la clave crece de largo.
    private static string Build(string? prev, string? next)
    {
        var result = new System.Text.StringBuilder();
        var depth = 0;

        while (true)
        {
            var prevDigit = depth < (prev?.Length ?? 0) ? IndexOf(prev![depth]) : 0;
            var nextExhausted = next is null || depth >= next.Length;
            var nextDigit = nextExhausted ? Base : IndexOf(next![depth]);

            if (nextDigit - prevDigit > 1)
            {
                var mid = prevDigit + (nextDigit - prevDigit) / 2;
                result.Append(Alphabet[mid]);
                return result.ToString();
            }

            result.Append(Alphabet[prevDigit]);
            depth++;
        }
    }

    private static int IndexOf(char c) => Alphabet.IndexOf(c);
}

public sealed class LexoRankRebalanceRequiredException : Exception
{
    public LexoRankRebalanceRequiredException()
        : base("El espacio disponible entre posiciones se agoto; la columna requiere rebalanceo.")
    {
    }
}
