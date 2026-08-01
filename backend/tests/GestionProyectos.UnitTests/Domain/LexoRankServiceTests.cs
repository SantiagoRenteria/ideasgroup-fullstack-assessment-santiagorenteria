using GestionProyectos.Domain.Common;
using Xunit;

namespace GestionProyectos.UnitTests.Domain;

// Unica prueba nombrada explicitamente por el enunciado (seccion 6.9): calculo de la
// nueva posicion de una tarea al reordenarla. Cubre los tres casos exigidos por el ADR
// (docs/decisions/arquitectura-decisiones.md §4): insercion normal, bordes de columna,
// y caso limite que fuerza rebalanceo.
public class LexoRankServiceTests
{
    [Fact]
    public void GetKeyBetween_ConDosClavesExistentes_DevuelveClaveIntermedia()
    {
        var key = LexoRankService.GetKeyBetween("M", "T");

        Assert.True(string.CompareOrdinal("M", key) < 0);
        Assert.True(string.CompareOrdinal(key, "T") < 0);
    }

    [Fact]
    public void GetKeyBetween_SinClavePrevia_InsertaAlInicioDeLaColumna()
    {
        var key = LexoRankService.GetKeyBetween(null, "M");

        Assert.True(string.CompareOrdinal(key, "M") < 0);
    }

    [Fact]
    public void GetKeyBetween_SinClaveSiguiente_InsertaAlFinalDeLaColumna()
    {
        var key = LexoRankService.GetKeyBetween("M", null);

        Assert.True(string.CompareOrdinal("M", key) < 0);
    }

    [Fact]
    public void GetKeyBetween_ColumnaVacia_DevuelveUnaClaveNoVacia()
    {
        var key = LexoRankService.GetKeyBetween(null, null);

        Assert.False(string.IsNullOrEmpty(key));
    }

    [Fact]
    public void GetKeyBetween_ClavesAdyacentesSinHueco_ProfundizaYSigueOrdenando()
    {
        // "a" y "b" son adyacentes en el alfabeto: no hay caracter unico intermedio,
        // el algoritmo debe extender la clave en vez de fallar o colisionar.
        var key = LexoRankService.GetKeyBetween("a", "b");

        Assert.True(string.CompareOrdinal("a", key) < 0);
        Assert.True(string.CompareOrdinal(key, "b") < 0);
        Assert.True(key.Length > 1);
    }

    [Fact]
    public void GetKeyBetween_PrevMayorOIgualQueNext_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() => LexoRankService.GetKeyBetween("T", "M"));
        Assert.Throws<ArgumentException>(() => LexoRankService.GetKeyBetween("M", "M"));
    }

    [Fact]
    public void GetKeyBetween_GapAgotadoTrasInsercionesRepetidas_LanzaRebalanceRequerido()
    {
        // Insertar siempre justo antes de la clave mas pequena existente agota el hueco
        // disponible y fuerza que la clave crezca sin limite -- el escenario de
        // rebalanceo que exige el ADR §4. Se prueba insertando repetidamente en el mismo
        // punto hasta que el servicio detecta que la clave excedio el largo maximo.
        string? smallest = "V";
        var rebalanceDetected = false;

        for (var i = 0; i < 200 && !rebalanceDetected; i++)
        {
            try
            {
                smallest = LexoRankService.GetKeyBetween(null, smallest);
            }
            catch (LexoRankRebalanceRequiredException)
            {
                rebalanceDetected = true;
            }
        }

        Assert.True(rebalanceDetected, "Se esperaba que el servicio senalizara la necesidad de rebalanceo.");
    }

    [Fact]
    public void GenerateSequence_ProduceClavesEnOrdenEstrictamenteAscendente()
    {
        var keys = LexoRankService.GenerateSequence(50);

        Assert.Equal(50, keys.Count);
        for (var i = 1; i < keys.Count; i++)
            Assert.True(string.CompareOrdinal(keys[i - 1], keys[i]) < 0);
    }

    [Fact]
    public void GenerateSequence_ConCeroElementos_DevuelveListaVacia()
    {
        var keys = LexoRankService.GenerateSequence(0);

        Assert.Empty(keys);
    }
}
