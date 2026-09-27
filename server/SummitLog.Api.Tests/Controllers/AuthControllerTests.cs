using SummitLog.Api.Controllers;
using SummitLog.Api.Dtos.Auth;
using SummitLog.Api.Models;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace SummitLog.Api.Tests.Controllers;

public class AuthControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough-1234567890",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpiryMinutes"] = "30"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JwtTokenService(config);
    }

    [Fact]
    public async Task Register_Success_ReturnsCreatedWithUserIdAndEmail()
    {
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = "new-user-id");

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Register(new RegisterRequest("new@example.com", "Password1!"));

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Register_Failure_ReturnsBadRequestWithErrors()
    {
        var userManager = CreateUserManagerMock();
        var errors = new[] { new IdentityError { Description = "Password too weak." } };
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Register(new RegisterRequest("bad@example.com", "weak"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var returnedErrors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequest.Value);
        Assert.Contains("Password too weak.", returnedErrors);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest("nobody@example.com", "whatever"));

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Login_LockedOutUser_ReturnsUnauthorizedWithoutCheckingPassword()
    {
        var user = new ApplicationUser { Id = "u1", Email = "locked@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest(user.Email!, "whatever"));

        Assert.IsType<UnauthorizedResult>(result.Result);
        userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedAndRecordsFailedAttempt()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest(user.Email!, "wrong"));

        Assert.IsType<UnauthorizedResult>(result.Result);
        userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsTokenAndResetsFailedCount()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

        var controller = new AuthController(userManager.Object, CreateJwtTokenService());

        var result = await controller.Login(new LoginRequest(user.Email!, "correct"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal(user.Email, response.Email);
        userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }
}
