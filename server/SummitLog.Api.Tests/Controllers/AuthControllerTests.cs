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

    private static AuthController CreateController(Mock<UserManager<ApplicationUser>> userManager, FakeEmailSender? emailSender = null)
    {
        var authEmailService = new AuthEmailService(emailSender ?? new FakeEmailSender(), "http://localhost:5173");
        return new AuthController(userManager.Object, CreateJwtTokenService(), authEmailService);
    }

    [Fact]
    public async Task Register_Success_ReturnsCreatedWithUserIdAndEmail()
    {
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = "new-user-id");
        userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("confirm-token");

        var controller = CreateController(userManager);

        var result = await controller.Register(new RegisterRequest("new@example.com", "Password1!"));

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Register_Success_SendsConfirmationEmail()
    {
        var userManager = CreateUserManagerMock();
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = "new-user-id");
        userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("confirm-token");
        var emailSender = new FakeEmailSender();

        var controller = CreateController(userManager, emailSender);

        await controller.Register(new RegisterRequest("new@example.com", "Password1!"));

        var sent = Assert.Single(emailSender.SentEmails);
        Assert.Equal("new@example.com", sent.ToEmail);
        Assert.Contains("confirm-token", sent.HtmlBody);
    }

    [Fact]
    public async Task Register_Failure_ReturnsBadRequestWithErrors()
    {
        var userManager = CreateUserManagerMock();
        var errors = new[] { new IdentityError { Description = "Password too weak." } };
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var controller = CreateController(userManager);

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

        var controller = CreateController(userManager);

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

        var controller = CreateController(userManager);

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

        var controller = CreateController(userManager);

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

        var controller = CreateController(userManager);

        var result = await controller.Login(new LoginRequest(user.Email!, "correct"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal(user.Email, response.Email);
        userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task Login_SoftDeletedUser_ReturnsUnauthorizedEvenWithCorrectPassword()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com", IsSoftDeleted = true };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

        var controller = CreateController(userManager);

        var result = await controller.Login(new LoginRequest(user.Email!, "correct"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConfirmEmail_Success_ConfirmsAndReactivatesSoftDeletedAccount()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com", IsSoftDeleted = true };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManager.Setup(m => m.ConfirmEmailAsync(user, "good-token")).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(userManager);

        var result = await controller.ConfirmEmail(new ConfirmEmailRequest("u1", "good-token"));

        Assert.IsType<OkResult>(result);
        Assert.False(user.IsSoftDeleted);
        userManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManager.Setup(m => m.ConfirmEmailAsync(user, "bad-token"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var controller = CreateController(userManager);

        var result = await controller.ConfirmEmail(new ConfirmEmailRequest("u1", "bad-token"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResendConfirmation_UnconfirmedUser_SendsEmail()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com", EmailConfirmed = false };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("new-token");
        var emailSender = new FakeEmailSender();

        var controller = CreateController(userManager, emailSender);

        await controller.ResendConfirmation(new ResendConfirmationRequest(user.Email!));

        Assert.Single(emailSender.SentEmails);
    }

    [Fact]
    public async Task ResendConfirmation_AlreadyConfirmedUser_DoesNotSendEmail()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com", EmailConfirmed = true };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        var emailSender = new FakeEmailSender();

        var controller = CreateController(userManager, emailSender);

        var result = await controller.ResendConfirmation(new ResendConfirmationRequest(user.Email!));

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task ResendConfirmation_UnknownEmail_StillReturnsOk()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager);

        var result = await controller.ResendConfirmation(new ResendConfirmationRequest("nobody@example.com"));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_SendsResetEmail()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        var emailSender = new FakeEmailSender();

        var controller = CreateController(userManager, emailSender);

        await controller.ForgotPassword(new ForgotPasswordRequest(user.Email!));

        Assert.Single(emailSender.SentEmails);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_StillReturnsOkWithoutSendingEmail()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        var emailSender = new FakeEmailSender();

        var controller = CreateController(userManager, emailSender);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest("nobody@example.com"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task ResetPassword_Success_ReturnsOk()
    {
        var user = new ApplicationUser { Id = "u1", Email = "user@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManager.Setup(m => m.ResetPasswordAsync(user, "good-token", "NewPassword1!")).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(userManager);

        var result = await controller.ResetPassword(new ResetPasswordRequest("u1", "good-token", "NewPassword1!"));

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_ReturnsBadRequest()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(userManager);

        var result = await controller.ResetPassword(new ResetPasswordRequest("missing", "token", "NewPassword1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
