using SummitLog.Api.Controllers;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace SummitLog.Api.Tests.Controllers;

public class ProfileControllerTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ProfileController CreateController(
        Mock<UserManager<ApplicationUser>> userManager,
        FakeAvatarStorage storage,
        string currentUserId = "user-1")
    {
        var controller = new ProfileController(userManager.Object, storage);
        controller.SetUser(currentUserId);
        return controller;
    }

    private static IFormFile CreateFormFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    private static readonly byte[] ValidPngHeader =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00];

    [Fact]
    public async Task GetProfile_UnknownUser_ReturnsNotFound()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var controller = CreateController(userManager, new FakeAvatarStorage());

        var result = await controller.GetProfile();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProfile_BlankNames_AreStoredAsNull()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com", FirstName = "Old", LastName = "Name" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(userManager, new FakeAvatarStorage());

        var result = await controller.UpdateProfile(new UpdateProfileRequest("  ", "  "));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Null(dto.FirstName);
        Assert.Null(dto.LastName);
    }

    [Fact]
    public async Task UpdateProfile_TrimsNameWhitespace()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(userManager, new FakeAvatarStorage());

        var result = await controller.UpdateProfile(new UpdateProfileRequest("  Alice  ", "  Smith  "));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
    }

    [Fact]
    public async Task UploadPicture_WrongExtension_ReturnsBadRequest()
    {
        var userManager = CreateUserManagerMock();
        var controller = CreateController(userManager, new FakeAvatarStorage());
        var file = CreateFormFile(ValidPngHeader, "malware.exe");

        var result = await controller.UploadPicture(file);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Unsupported file type", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task UploadPicture_ContentDoesNotMatchExtension_ReturnsBadRequest()
    {
        var userManager = CreateUserManagerMock();
        var controller = CreateController(userManager, new FakeAvatarStorage());
        var textDisguisedAsPng = CreateFormFile("this is just plain text, not an image"u8.ToArray(), "fake.png");

        var result = await controller.UploadPicture(textDisguisedAsPng);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("does not match", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task UploadPicture_ValidPng_SavesFileAndReturnsUpdatedProfile()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var storage = new FakeAvatarStorage();
        var controller = CreateController(userManager, storage);
        var file = CreateFormFile(ValidPngHeader, "avatar.png");

        var result = await controller.UploadPicture(file);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("/api/profile/user-1/avatar", dto.ProfilePictureUrl);
        Assert.True(storage.Files.ContainsKey("user-1.png"));
    }

    [Fact]
    public async Task UploadPicture_ReplacingExistingPicture_DeletesPreviousFile()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com", ProfilePicturePath = "user-1.jpg" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var storage = new FakeAvatarStorage();
        await storage.SaveAsync("user-1.jpg", new MemoryStream([0xFF, 0xD8, 0xFF]), "image/jpeg");
        var controller = CreateController(userManager, storage);

        var newFile = CreateFormFile(ValidPngHeader, "avatar.png");

        await controller.UploadPicture(newFile);

        Assert.False(storage.Files.ContainsKey("user-1.jpg"));
        Assert.True(storage.Files.ContainsKey("user-1.png"));
    }

    [Fact]
    public async Task DeletePicture_ExistingPicture_DeletesFileAndClearsPath()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com", ProfilePicturePath = "user-1.jpg" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var storage = new FakeAvatarStorage();
        await storage.SaveAsync("user-1.jpg", new MemoryStream([0xFF, 0xD8, 0xFF]), "image/jpeg");
        var controller = CreateController(userManager, storage);

        var result = await controller.DeletePicture();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Null(dto.ProfilePictureUrl);
        Assert.Null(user.ProfilePicturePath);
        Assert.False(storage.Files.ContainsKey("user-1.jpg"));
    }

    [Fact]
    public async Task DeletePicture_NoExistingPicture_IsNoOp()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@example.com" };
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        var controller = CreateController(userManager, new FakeAvatarStorage());

        var result = await controller.DeletePicture();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Null(dto.ProfilePictureUrl);
        userManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DeletePicture_UnknownUser_ReturnsNotFound()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);
        var controller = CreateController(userManager, new FakeAvatarStorage());

        var result = await controller.DeletePicture();

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
