using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;

using CRM.Server.Controllers;
using CRM.Server.Models;
using CRM.Server.DTOs.Auth;
using CRM.Server.Security;
using CRM.Server.Services.Interfaces;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IAuditLogService> _audit = new();
    private readonly Mock<IRefreshTokenService> _refresh = new();
    private readonly Mock<IUserSessionService> _session = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userManager = CreateUserManagerMock();

        var refreshOpts = Options.Create(new RefreshTokenSettings
        {
            RefreshTokenExpiryDays = 30,
            RotationEnabled = true
        });

        var jwtOpts = Options.Create(new JwtSettings
        {
            ExpiryMinutes = 60
        });

        var sessionOpts = Options.Create(new SessionSettings
        {
            AbsoluteSessionLifetimeMinutes = 120
        });

        _controller = new AuthController(
            _userManager.Object,
            _jwt.Object,
            _userService.Object,
            _audit.Object,
            _refresh.Object,
            refreshOpts,
            jwtOpts,
            _session.Object,
            sessionOpts,
            _logger.Object
        );

        // Mock HttpContext + IP + User-Agent
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        _controller.HttpContext.Request.Headers["User-Agent"] = "UnitTest-Agent";
    }

    // --------------------------------------------
    // 1. User not found → Unauthorized
    // --------------------------------------------
    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        _userManager.Setup(x => x.FindByEmailAsync("test@mail.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = "test@mail.com",
            Password = "123"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // --------------------------------------------
    // 2. User inactive → Unauthorized
    // --------------------------------------------
    [Fact]
    public async Task Login_UserInactive_ReturnsUnauthorized()
    {
        var user = new ApplicationUser { Email = "test@mail.com", IsActive = false };

        _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "123"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // --------------------------------------------
    // 3. Invalid password → Unauthorized
    // --------------------------------------------
    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var user = new ApplicationUser { Email = "test@mail.com", IsActive = true };

        _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "wrong"))
            .ReturnsAsync(false);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "wrong"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // --------------------------------------------
    // 4. MFA required → Returns MFA prompt
    // --------------------------------------------
    [Fact]
    public async Task Login_MfaEnabled_ReturnsMfaRequired()
    {
        var user = new ApplicationUser
        {
            Email = "test@mail.com",
            IsActive = true,
            TwoFactorEnabled = true
        };

        _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "123")).ReturnsAsync(true);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "123"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);

        Assert.True(dto.MfaRequired);
        Assert.Equal(user.Email, dto.Email);
    }

    // --------------------------------------------
    // 5. Password expired → 403 Forbidden
    // --------------------------------------------
    [Fact]
    public async Task Login_PasswordExpired_Returns403()
    {
        var user = new ApplicationUser
        {
            Id = "U1",
            Email = "expired@mail.com",
            IsActive = true
        };

        _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "123")).ReturnsAsync(true);

        _userService.Setup(x => x.IsPasswordExpiredAsync(user))
            .ReturnsAsync(true);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "123"
        });

        var res = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, res.StatusCode);
    }

    // --------------------------------------------
    // 6. Login success → Returns JWT + Refresh token
    // --------------------------------------------
    [Fact]
    public async Task Login_Success_ReturnsTokens()
    {
        var user = new ApplicationUser
        {
            Id = "U1",
            Email = "active@mail.com",
            IsActive = true
        };

        _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, "123")).ReturnsAsync(true);
        _userService.Setup(x => x.IsPasswordExpiredAsync(user)).ReturnsAsync(false);

        _userManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        var session = new UserSession { Id = "SID1" };
        _session.Setup(x => x.CreateSessionAsync(user.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(session);

        _jwt.Setup(x => x.GenerateToken(user, It.IsAny<IList<string>>(), "SID1"))
     .Returns("jwt-token");

        var refreshToken = new RefreshToken
        {
            Id = "R1",
            Token = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _refresh.Setup(x => x.CreateRefreshTokenAsync(user.Id, It.IsAny<string>(), It.IsAny<string>(), 30, "SID1"))
            .ReturnsAsync(refreshToken);

        var result = await _controller.Login(new LoginRequestDto
        {
            Email = user.Email,
            Password = "123"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<AuthResponseDto>(ok.Value);

        Assert.Equal("jwt-token", dto.Token);
        Assert.Equal("refresh-token", dto.RefreshToken);
    }

    // --------------------------------------------
    // Helper: Create mock UserManager
    // --------------------------------------------
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null, null, null, null, null, null, null, null
        );
    }
}
