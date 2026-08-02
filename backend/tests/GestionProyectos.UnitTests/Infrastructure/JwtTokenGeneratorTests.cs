using System.IdentityModel.Tokens.Jwt;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace GestionProyectos.UnitTests.Infrastructure;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        Secret = "clave-de-prueba-suficientemente-larga-para-hmac-sha256",
        Issuer = "GestionProyectos.Tests",
        Audience = "GestionProyectos.Tests.Client",
        ExpirationMinutes = 30
    };

    [Fact]
    public void Generate_IncluyeClaimsEIssuerAudienceCorrectos()
    {
        var generator = new JwtTokenGenerator(Options.Create(_options));
        var user = new User(Guid.NewGuid(), "Ana Perez", "ana@ideasgroup.test", "hash-irrelevante");

        var token = generator.Generate(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(_options.Issuer, jwt.Issuer);
        Assert.Equal(_options.Audience, jwt.Audiences.Single());
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email.Value, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
    }
}
