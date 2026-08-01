using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Users;

public static class UserMappingExtensions
{
    public static UserResponseDto ToDto(this User user) => new(user.Id, user.Name, user.Email);
}
