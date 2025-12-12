using Xunit;
using Moq;
using CRM.Server.Services;
using CRM.Server.Services.Interfaces;
using CRM.Server.Repositories.Interfaces;
using CRM.Server.Models;
using CRM.Server.DTOs.Users;
using CRM.Server.Security;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();

    private readonly UserService _service;

    public UserServiceTests()
    {
        var policy = new PasswordPolicySettings
        {
            PasswordExpiryMinutes = 60
        };

        _userManager = MockUserManager();

        // Real configuration object (correct type!)
        var configData = new Dictionary<string, string>
    {
        { "Client:Url", "https://localhost" }
    };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new UserService(
            _userRepo.Object,
            _userManager.Object,
            _emailService.Object,
            _config,                // correct type now
            _auditLogService.Object,
            _roleRepo.Object,
            Options.Create(policy)
        );
    }


    // --------------------------------------------------------------------
    // TEST 1: IsPasswordExpiredAsync
    // --------------------------------------------------------------------

    [Fact]
    public async Task IsPasswordExpiredAsync_ReturnsTrue_WhenExpired()
    {
        var user = new ApplicationUser
        {
            PasswordLastChanged = DateTime.UtcNow.AddHours(-5)
        };

        var result = await _service.IsPasswordExpiredAsync(user);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_ReturnsFalse_WhenNotExpired()
    {
        var user = new ApplicationUser
        {
            PasswordLastChanged = DateTime.UtcNow.AddMinutes(-10)
        };

        var result = await _service.IsPasswordExpiredAsync(user);

        Assert.False(result);
    }

    // --------------------------------------------------------------------
    // TEST 2: AssignRoleAsync
    // --------------------------------------------------------------------

    [Fact]
    public async Task AssignRoleAsync_AssignsRole_AndLogsAudit()
    {
        var user = new ApplicationUser { Id = "U1", Email = "test@mail.com" };

        _userManager.Setup(x => x.FindByIdAsync("U1"))
            .ReturnsAsync(user);

        // RETURN IdentityRole INSTEAD OF ApplicationRole
        _roleRepo.Setup(x => x.GetByNameAsync("Admin"))
            .ReturnsAsync(new IdentityRole("Admin"));

        _userManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        _userManager.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.AssignRoleAsync("U1", "Admin", "PerformedBy");

        // Assert
        _auditLogService.Verify(x =>
            x.LogAsync(
                "PerformedBy",
                "U1",
                "Role Assigned",
                "User",
                true,
                null,
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
    }

    // --------------------------------------------------------------------
    // TEST 3: CreateUserAsync
    // --------------------------------------------------------------------

    [Fact]
    public async Task CreateUserAsync_CreatesUser_AndSendsEmail()
    {
        var dto = new CreateUserDto
        {
            FullName = "Test User",
            Email = "test@mail.com",
            Role = "Admin"
        };

        _userManager.Setup(x => x.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.CreateUserAsync(dto, "AdminUser");

        // Assert: Email sent
        _emailService.Verify(x =>
            x.SendAsync(dto.Email, "Your CRM Account Credentials", It.IsAny<string>()),
            Times.Once);

        // Assert: Audit logged
        _auditLogService.Verify(x =>
            x.LogAsync(
                "AdminUser",
                It.IsAny<string>(),
                "User Created",
                "User",
                true,
                null,
                null,
                It.IsAny<string>()
        ), Times.Once);
    }

    // --------------------------------------------------------------------
    // TEST 4: ChangePasswordAsync
    // --------------------------------------------------------------------

    [Fact]
    public async Task ChangePasswordAsync_UpdatesPasswordTimestamp()
    {
        var user = new ApplicationUser { Id = "U1", Email = "mail@mail.com" };

        _userManager.Setup(x => x.FindByIdAsync("U1"))
            .ReturnsAsync(user);

        _userManager.Setup(x => x.ChangePasswordAsync(user, "oldpass", "newpass"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _service.ChangePasswordAsync("U1", "oldpass", "newpass");

        // Assert
        Assert.True((DateTime.UtcNow - user.PasswordLastChanged).TotalSeconds < 5);

        _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null, null, null, null, null, null, null, null
        );
    }
}
