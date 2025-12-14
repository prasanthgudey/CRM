using CRM.Server.Controllers;
using CRM.Server.DTOs.Auth;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();

        _controller = new AuthController(_authServiceMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // --------------------------------------------
    // LOGIN SUCCESS
    // --------------------------------------------
    [Fact]
    public async Task Login_WhenSuccess_ReturnsOk()
    {
        var response = new AuthResponseDto
        {
            Token = "jwt",
            RefreshToken = "refresh"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<HttpContext>()))
            .ReturnsAsync(response);

        var result = await _controller.Login(new LoginRequestDto());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    // --------------------------------------------
    // PASSWORD EXPIRED → 403
    // --------------------------------------------
    [Fact]
    public async Task Login_PasswordExpired_Returns403()
    {
        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<HttpContext>()))
            .ThrowsAsync(new AuthPasswordExpiredException());

        var result = await _controller.Login(new LoginRequestDto());

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(403);
    }

    // --------------------------------------------
    // MFA REQUIRED
    // --------------------------------------------
    [Fact]
    public async Task Login_MfaRequired_ReturnsMfaResponse()
    {
        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<HttpContext>()))
            .ThrowsAsync(new AuthMfaRequiredException("test@mail.com"));

        var result = await _controller.Login(new LoginRequestDto());

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        dynamic value = ok.Value!;

        Assert.True(value.mfaRequired);
        Assert.Equal("test@mail.com", value.email);
    }

    // --------------------------------------------
    // INVALID CREDENTIALS
    // --------------------------------------------
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        _authServiceMock
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<HttpContext>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _controller.Login(new LoginRequestDto());

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --------------------------------------------
    // MFA LOGIN
    // --------------------------------------------
    [Fact]
    public async Task MfaLogin_ReturnsOk()
    {
        _authServiceMock
            .Setup(s => s.MfaLoginAsync(It.IsAny<MfaLoginDto>(), It.IsAny<HttpContext>()))
            .ReturnsAsync(new AuthResponseDto());

        var result = await _controller.MfaLogin(new MfaLoginDto());

        result.Should().BeOfType<OkObjectResult>();
    }

    // --------------------------------------------
    // REFRESH TOKEN EXPIRED
    // --------------------------------------------
    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        _authServiceMock
            .Setup(s => s.RefreshAsync(It.IsAny<RefreshRequestDto>(), It.IsAny<HttpContext>()))
            .ThrowsAsync(new AuthTokenExpiredException());

        var result = await _controller.Refresh(new RefreshRequestDto());

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --------------------------------------------
    // LOGOUT
    // --------------------------------------------
    [Fact]
    public async Task Logout_ReturnsOk()
    {
        _authServiceMock
            .Setup(s => s.LogoutAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        result.Should().BeOfType<OkObjectResult>();
    }
}
