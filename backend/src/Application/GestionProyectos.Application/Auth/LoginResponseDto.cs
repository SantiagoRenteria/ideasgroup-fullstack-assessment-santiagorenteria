namespace GestionProyectos.Application.Auth;

public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Nombre, string Correo);
