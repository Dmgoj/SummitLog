using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SummitLog.Api.Models;
using SummitLog.Api.Services;
using Microsoft.Extensions.Configuration;

namespace SummitLog.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough-1234567890",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpiryMinutes"] = "30"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ApplicationUser CreateUser() => new()
    {
        Id = "user-123",
        Email = "someone@example.com",
        UserName = "someone@example.com"
    };

    [Fact]
    public void CreateToken_IncludesExpectedClaims()
    {
        var service = new JwtTokenService(BuildConfig());
        var user = CreateUser();

        var (token, _) = service.CreateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
    }

    [Fact]
    public void CreateToken_ExpiresAtMatchesConfiguredMinutes()
    {
        var service = new JwtTokenService(BuildConfig());
        var before = DateTime.UtcNow;

        var (_, expiresAt) = service.CreateToken(CreateUser());

        var expectedMin = before.AddMinutes(30);
        var expectedMax = DateTime.UtcNow.AddMinutes(30);
        Assert.InRange(expiresAt, expectedMin, expectedMax);
    }

    [Fact]
    public void CreateToken_MissingKey_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Jwt:Key"] = null });
        var service = new JwtTokenService(config);

        Assert.Throws<InvalidOperationException>(() => service.CreateToken(CreateUser()));
    }
}
