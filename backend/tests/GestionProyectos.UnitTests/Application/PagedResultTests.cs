using GestionProyectos.Application.Common.Models;
using Xunit;

namespace GestionProyectos.UnitTests.Application;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_ConTotalMultiploExactoDePageSize_NoRedondeaHaciaArriba()
    {
        var result = new PagedResult<int>([1, 2], page: 1, pageSize: 10, totalCount: 20);

        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ConResto_RedondeaHaciaArriba()
    {
        var result = new PagedResult<int>([1, 2], page: 1, pageSize: 10, totalCount: 21);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_SinResultados_EsCero()
    {
        var result = new PagedResult<int>([], page: 1, pageSize: 10, totalCount: 0);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ConPageSizeCero_NoLanzaExcepcionYEsCero()
    {
        var result = new PagedResult<int>([], page: 1, pageSize: 0, totalCount: 5);

        Assert.Equal(0, result.TotalPages);
    }
}
