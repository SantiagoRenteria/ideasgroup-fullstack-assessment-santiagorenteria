namespace GestionProyectos.Domain.Common;

// LexoRank simplificado (ADR §4): genera claves ordenables entre dos posiciones sin
// reescribir el resto de la columna. Alfabeto base62 ya en orden ASCII (0-9 < A-Z < a-z).
public static class LexoRankService
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Base = 62;

    // Fuerza un rebalanceo en vez de dejar crecer la clave sin limite: insertar siempre
    // en el mismo extremo agota el hueco disponible caracter a caracter.
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

    // Reutiliza el mismo algoritmo de punto medio por biseccion para generar `count`
    // claves: cubre tanto la insercion normal como el rebalanceo de una columna.
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

    // Punto medio caracter a caracter: si hay hueco se resuelve con un digito intermedio;
    // si son adyacentes, conserva el digito de `prev` y profundiza una posicion mas.
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
