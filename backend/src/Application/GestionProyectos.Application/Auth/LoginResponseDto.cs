namespace GestionProyectos.Application.Auth;

public record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Name, string Email);
