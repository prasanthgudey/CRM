//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using CRM.Server.Controllers;
//using CRM.Server.DTOs.Auth;
//using CRM.Server.Models;
//using CRM.Server.Services.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using Xunit;

//namespace CRM.Tests.Controllers
//{
//    public class AuthControllerTests
//    {
//        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
//        private readonly Mock<IJwtTokenService> _jwtMock;
//        private readonly Mock<IUserService> _userServiceMock;
//        private readonly Mock<IAuditLogService> _auditMock;
//        private readonly AuthController _controller;

//        public AuthControllerTests()
//        {
//            _userManagerMock = MockUserManager();
//            _jwtMock = new Mock<IJwtTokenService>();
//            _userServiceMock = new Mock<IUserService>();
//            _auditMock = new Mock<IAuditLogService>();

//            _controller = new AuthController(
//                _userManagerMock.Object,
//                _jwtMock.Object,
//                _userServiceMock.Object,
//                _auditMock.Object);

//            // Fake request context for IP
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext()
//                {
//                    Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("1.2.3.4") }
//                }
//            };
//        }

//        // Helper to mock UserManager
//        private Mock<UserManager<ApplicationUser>> MockUserManager()
//        {
//            var store = new Mock<IUserStore<ApplicationUser>>();
//            return new Mock<UserManager<ApplicationUser>>(
//                store.Object, null, null, null, null, null, null, null, null
//            );
//        }

//        private ApplicationUser User(string id = "u1", string email = "test@crm.com", bool active = true)
//        {
//            return new ApplicationUser
//            {
//                Id = id,
//                Email = email,
//                UserName = email,
//                IsActive = active,
//                TwoFactorEnabled = false
//            };
//        }

//        // ---------------------------------------------------------
//        // 1) LOGIN SUCCESS
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task Login_WhenValid_ReturnsToken()
//        {
//            var user = User();
//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);
//            _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
//            _userManagerMock.Setup(u => u.GetRolesAsync(user))
//                .ReturnsAsync(new List<string> { "Admin" });

//            _jwtMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>()))
//        .Returns("jwt_token");


//            var result = await _controller.Login(new LoginRequestDto
//            {
//                Email = "test@crm.com",
//                Password = "pass"
//            }) as OkObjectResult;

//            Assert.NotNull(result);
//            var dto = result.Value as AuthResponseDto;
//            Assert.Equal("jwt_token", dto!.Token);

//            _auditMock.Verify(a => a.LogAsync(
//                "u1",
//                null,
//                "Login Success",
//                "Authentication",
//                true,
//                "1.2.3.4",
//                null,
//                null
//            ), Times.Once);
//        }

//        // ---------------------------------------------------------
//        // 2) LOGIN INVALID PASSWORD
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task Login_WhenInvalidPassword_ReturnsUnauthorized()
//        {
//            var user = User();
//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);
//            _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "pass")).ReturnsAsync(false);

//            var result = await _controller.Login(new LoginRequestDto
//            {
//                Email = "test@crm.com",
//                Password = "pass"
//            });

//            Assert.IsType<UnauthorizedObjectResult>(result);

//            _auditMock.Verify(a => a.LogAsync(
//                "u1",
//                null,
//                "Login Failed",
//                "Authentication",
//                false,
//                "1.2.3.4",
//                null,
//                "Invalid password"
//            ), Times.Once);
//        }

//        // ---------------------------------------------------------
//        // 3) LOGIN INACTIVE USER
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task Login_WhenUserInactive_ReturnsUnauthorized()
//        {
//            var user = User(active: false);
//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);

//            var result = await _controller.Login(new LoginRequestDto
//            {
//                Email = "test@crm.com",
//                Password = "pass"
//            });

//            Assert.IsType<UnauthorizedObjectResult>(result);

//            _auditMock.Verify(a => a.LogAsync(
//                "u1",
//                null,
//                "Login Failed",
//                "Authentication",
//                false,
//                "1.2.3.4",
//                null,
//                "Account is deactivated"
//            ));
//        }

//        // ---------------------------------------------------------
//        // 4) LOGIN WITH MFA REQUIRED
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task Login_WhenMfaEnabled_ReturnsMfaRequired()
//        {
//            var user = User();
//            user.TwoFactorEnabled = true;

//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);
//            _userManagerMock.Setup(u => u.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);

//            var result = await _controller.Login(new LoginRequestDto
//            {
//                Email = "test@crm.com",
//                Password = "pass"
//            }) as OkObjectResult;

//            Assert.NotNull(result);
//            var dto = result.Value as AuthResponseDto;
//            Assert.True(dto!.MfaRequired);
//        }

//        // ---------------------------------------------------------
//        // 5) MFA LOGIN SUCCESS
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task MfaLogin_WhenValid_ReturnsToken()
//        {
//            var user = User();
//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);

//            _userManagerMock.Setup(u => u.VerifyTwoFactorTokenAsync(
//                user,
//                TokenOptions.DefaultAuthenticatorProvider,
//                "123456"))
//                .ReturnsAsync(true);

//            _userManagerMock.Setup(u => u.GetRolesAsync(user))
//                .ReturnsAsync(new List<string> { "Admin" });

//            _jwtMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>()))
//         .Returns("jwt_token");


//            var result = await _controller.MfaLogin(new MfaLoginDto
//            {
//                Email = "test@crm.com",
//                Code = "123456"
//            }) as OkObjectResult;

//            Assert.NotNull(result);
//            var dto = result.Value as AuthResponseDto;
//            Assert.Equal("jwt_token", dto!.Token);
//        }

//        // ---------------------------------------------------------
//        // 6) MFA LOGIN INVALID
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task MfaLogin_WhenInvalidCode_ReturnsUnauthorized()
//        {
//            var user = User();
//            _userManagerMock.Setup(u => u.FindByEmailAsync("test@crm.com")).ReturnsAsync(user);

//            _userManagerMock.Setup(u => u.VerifyTwoFactorTokenAsync(
//                user,
//                TokenOptions.DefaultAuthenticatorProvider,
//                "111"))
//                .ReturnsAsync(false);

//            var result = await _controller.MfaLogin(new MfaLoginDto
//            {
//                Email = "test@crm.com",
//                Code = "111"
//            });

//            Assert.IsType<UnauthorizedObjectResult>(result);
//        }

//        // ---------------------------------------------------------
//        // 7) LOGOUT
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task Logout_LogsAudit()
//        {
//            // Mock user identity
//            var claims = new ClaimsIdentity(new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, "u1")
//            });

//            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(claims);

//            var result = await _controller.Logout() as OkObjectResult;

//            Assert.NotNull(result);

//            _auditMock.Verify(a => a.LogAsync(
//                "u1",
//                null,
//                "Logout",
//                "Authentication",
//                true,
//                "1.2.3.4",
//                null,
//                null
//            ), Times.Once);
//        }

//        // ---------------------------------------------------------
//        // 8) CHANGE PASSWORD
//        // ---------------------------------------------------------
//        [Fact]
//        public async Task ChangePassword_ReturnsOk_AndAudits()
//        {
//            var claims = new ClaimsIdentity(new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, "u1")
//            });

//            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(claims);

//            var dto = new ChangePasswordDto
//            {
//                CurrentPassword = "old",
//                NewPassword = "new"
//            };

//            var result = await _controller.ChangePassword(dto);

//            Assert.IsType<OkObjectResult>(result);

//            _auditMock.Verify(a => a.LogAsync(
//                "u1",
//                null,
//                "Password Changed",
//                "Authentication",
//                true,
//                "1.2.3.4",
//                null,
//                null
//            ), Times.Once);
//        }
//    }
//}
