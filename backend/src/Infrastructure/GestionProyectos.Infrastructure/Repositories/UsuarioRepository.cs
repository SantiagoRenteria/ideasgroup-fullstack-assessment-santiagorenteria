using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _dbContext;

    public UsuarioRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Usuario?> GetByCorreoAsync(string correo, CancellationToken cancellationToken) =>
        _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Correo == correo, cancellationToken);
}
