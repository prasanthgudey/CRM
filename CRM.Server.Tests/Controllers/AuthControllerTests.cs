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
//using FluentAssertions;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//namespace CRM.Server.Tests.Controllers
//{
//    public class AuthControllerTests
//    {
//        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
//        private readonly Mock<IJwtTokenService> _jwtMock;
//        private readonly Mock<IUserService> _userServiceMock;
//        private readonly AuthController _controller;

//        public AuthControllerTests()
//        {
//            _userManagerMock = CreateUserManagerMock();
//            _jwtMock = new Mock<IJwtTokenService>();
//            _userServiceMock = new Mock<IUserService>();

//            _controller = new AuthController(_userManagerMock.Object, _jwtMock.Object, _userServiceMock.Object)
//            {
//                ControllerContext = new ControllerContext
//                {
//                    HttpContext = new DefaultHttpContext()
//                }
//            };
//        }

//        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
//        {
//            var store = new Mock<IUserStore<ApplicationUser>>();
//            var mgr = new Mock<UserManager<ApplicationUser>>(
//                store.Object,
//                Mock.Of<IOptions<IdentityOptions>>(),
//                Mock.Of<IPasswordHasher<ApplicationUser>>(),
//                Array.Empty<IUserValidator<ApplicationUser>>(),
//                Array.Empty<IPasswordValidator<ApplicationUser>>(),
//                Mock.Of<ILookupNormalizer>(),
//                Mock.Of<IdentityErrorDescriber>(),
//                Mock.Of<IServiceProvider>(),
//                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
//            );

//            // By default, return IdentityResult.Success for resets/updates unless overridden.
//            mgr.Setup(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
//               .ReturnsAsync(IdentityResult.Success);

//            mgr.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
//               .ReturnsAsync(IdentityResult.Success);

//            return mgr;
//        }

//        // ---------------------------
//        // LOGIN - SUCCESS (no MFA)
//        // ---------------------------
//        [Fact]
//        public async Task Login_Should_Return_Token_When_Credentials_Valid_And_No_Mfa()
//        {
//            // Arrange
//            var dto = new LoginRequestDto { Email = "a@e.com", Password = "P@ssw0rd" };
//            var user = new ApplicationUser { Email = dto.Email, UserName = dto.Email, IsActive = true };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
//            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

//            _jwtMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>())).Returns("TOKEN123");

//            // Act
//            var result = await _controller.Login(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeOfType<AuthResponseDto>();
//            var resp = (AuthResponseDto)ok.Value;
//            resp.Token.Should().Be("TOKEN123");
//            resp.MfaRequired.Should().BeFalse();
//            resp.Expiration.Should().NotBeNull();
//        }

//        // ---------------------------
//        // LOGIN - MFA required
//        // ---------------------------
//        [Fact]
//        public async Task Login_Should_Return_MfaRequired_When_User_Has_MfaEnabled()
//        {
//            // Arrange
//            var dto = new LoginRequestDto { Email = "a@e.com", Password = "P@ss" };
//            var user = new ApplicationUser { Email = dto.Email, UserName = dto.Email, IsActive = true, TwoFactorEnabled = true };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);

//            // Act
//            var result = await _controller.Login(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeOfType<AuthResponseDto>();
//            var resp = (AuthResponseDto)ok.Value;
//            resp.MfaRequired.Should().BeTrue();
//            resp.Email.Should().Be(dto.Email);
//            resp.Token.Should().BeNull();
//        }

//        // ---------------------------
//        // LOGIN - Unauthorized (invalid credentials)
//        // ---------------------------
//        [Fact]
//        public async Task Login_Should_Return_Unauthorized_When_Invalid_Credentials()
//        {
//            // Arrange
//            var dto = new LoginRequestDto { Email = "noone@e.com", Password = "x" };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((ApplicationUser?)null);

//            // Act
//            var result = await _controller.Login(dto);

//            // Assert
//            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
//            unauth.Value.Should().Be("Invalid credentials");
//        }

//        // ---------------------------
//        // COMPLETE REGISTRATION - success
//        // ---------------------------
//        [Fact]
//        public async Task CompleteRegistration_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var token = "token-encoded";
//            var dto = new CompleteRegistrationDto { Email = "invite@e.com", Token = Uri.EscapeDataString(token), Password = "NewP@ss1" };

//            var user = new ApplicationUser
//            {
//                Email = dto.Email,
//                IsInvitePending = true,
//                InviteExpiry = DateTime.UtcNow.AddHours(1)
//            };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, token, dto.Password)).ReturnsAsync(IdentityResult.Success);
//            _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

//            // Act
//            var result = await _controller.CompleteRegistration(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var successProp = ok.Value.GetType().GetProperty("success");
//            var messageProp = ok.Value.GetType().GetProperty("message");
//            ((bool)successProp!.GetValue(ok.Value)!).Should().BeTrue();
//            ((string)messageProp!.GetValue(ok.Value)!).Should().Be("Registration completed successfully");
//        }

//        // ---------------------------
//        // COMPLETE REGISTRATION - expired invite -> BadRequest
//        // ---------------------------
//        [Fact]
//        public async Task CompleteRegistration_Should_Return_BadRequest_When_Expired()
//        {
//            // Arrange
//            var dto = new CompleteRegistrationDto { Email = "expired@e.com", Token = "t", Password = "p" };
//            var user = new ApplicationUser { Email = dto.Email, IsInvitePending = true, InviteExpiry = DateTime.UtcNow.AddHours(-1) };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);

//            // Act
//            var result = await _controller.CompleteRegistration(dto);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().Be("Invitation link has expired");
//        }

//        // ---------------------------
//        // FORGOT PASSWORD - forwards to IUserService (Ok)
//        // ---------------------------
//        [Fact]
//        public async Task ForgotPassword_Should_Return_Ok()
//        {
//            // Arrange
//            var dto = new ForgotPasswordDto { Email = "me@e.com" };
//            _userServiceMock.Setup(s => s.ForgotPasswordAsync(dto.Email)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.ForgotPassword(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(new { message = "If the email is registered, a reset link has been sent." });
//        }

//        // ---------------------------
//        // RESET PASSWORD - forwards to IUserService
//        // ---------------------------
//        [Fact]
//        public async Task ResetPassword_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var dto = new ResetPasswordDto { Email = "me@e.com", Token = "t", NewPassword = "NewP@ss" };
//            _userServiceMock.Setup(s => s.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.ResetPassword(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(new { message = "Password reset successful" });
//        }

//        // ---------------------------
//        // CHANGE PASSWORD - authorized user
//        // ---------------------------
//        [Fact]
//        public async Task ChangePassword_Should_Return_Ok_For_Authenticated_User()
//        {
//            // Arrange
//            var userId = "user-1";
//            var dto = new ChangePasswordDto { CurrentPassword = "c", NewPassword = "n12345" };

//            // put claim
//            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
//            _controller.ControllerContext.HttpContext.User = claims;

//            _userServiceMock.Setup(s => s.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.ChangePassword(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(new { message = "Password changed successfully" });
//        }

//        // ---------------------------
//        // ENABLE MFA - authorized - returns DTO from service
//        // ---------------------------
//        [Fact]
//        public async Task EnableMfa_Should_Return_EnableMfaResponse()
//        {
//            // Arrange
//            var userId = "u1";
//            var response = new EnableMfaResponseDto { SharedKey = "KEY", QrCodeImageUrl = "URL" };

//            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
//            _controller.ControllerContext.HttpContext.User = claims;

//            _userServiceMock.Setup(s => s.EnableMfaAsync(userId)).ReturnsAsync(response);

//            // Act
//            var result = await _controller.EnableMfa();

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(response);
//        }

//        // ---------------------------
//        // VERIFY MFA - authorized - service does not throw
//        // ---------------------------
//        [Fact]
//        public async Task VerifyMfa_Should_Return_Ok_When_Code_Valid()
//        {
//            // Arrange
//            var userId = "u1";
//            var dto = new VerifyMfaDto { Code = "123456" };

//            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
//            _controller.ControllerContext.HttpContext.User = claims;

//            _userServiceMock.Setup(s => s.VerifyMfaAsync(userId, dto.Code)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.VerifyMfa(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(new { message = "MFA enabled successfully" });
//        }

//        // ---------------------------
//        // DISABLE MFA - authorized - success
//        // ---------------------------
//        [Fact]
//        public async Task DisableMfa_Should_Return_Ok_When_Code_Valid()
//        {
//            // Arrange
//            var userId = "u1";
//            var dto = new VerifyMfaDto { Code = "123456" };

//            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
//            _controller.ControllerContext.HttpContext.User = claims;

//            _userServiceMock.Setup(s => s.DisableMfaAsync(userId, dto.Code)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.DisableMfa(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(new { message = "MFA disabled successfully" });
//        }

//        // ---------------------------
//        // MFA LOGIN - SUCCESS -> returns token
//        // ---------------------------
//        [Fact]
//        public async Task MfaLogin_Should_Return_Token_When_Code_Valid()
//        {
//            // Arrange
//            var dto = new MfaLoginDto { Email = "mfa@e.com", Code = "123456" };
//            var user = new ApplicationUser { Email = dto.Email, UserName = dto.Email, IsActive = true };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, dto.Code)).ReturnsAsync(true);
//            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
//            _jwtMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>())).Returns("MFATOKEN");

//            // Act
//            var result = await _controller.MfaLogin(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeOfType<AuthResponseDto>();
//            var resp = (AuthResponseDto)ok.Value;
//            resp.Token.Should().Be("MFATOKEN");
//        }

//        // ---------------------------
//        // MFA LOGIN - FAILURE -> Unauthorized on invalid code
//        // ---------------------------
//        [Fact]
//        public async Task MfaLogin_Should_Return_Unauthorized_When_Code_Invalid()
//        {
//            // Arrange
//            var dto = new MfaLoginDto { Email = "mfa@e.com", Code = "000000" };
//            var user = new ApplicationUser { Email = dto.Email, UserName = dto.Email, IsActive = true };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, dto.Code)).ReturnsAsync(false);

//            // Act
//            var result = await _controller.MfaLogin(dto);

//            // Assert
//            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
//            unauth.Value.Should().Be("Invalid verification code");
//        }

//        // ---------------------------
//        // PRESENTATION - meaningful failing test
//        // Demonstrates when ResetPassword fails (invalid token)
//        // ---------------------------
//        [Fact]
//        public async Task CompleteRegistration_PresentationFail_InvalidToken_Shows_Failure()
//        {
//            // Arrange
//            var dto = new CompleteRegistrationDto { Email = "a@e.com", Token = "t", Password = "P@ss1" };
//            var user = new ApplicationUser { Email = dto.Email, IsInvitePending = true, InviteExpiry = DateTime.UtcNow.AddHours(1) };

//            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
//            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), dto.Password))
//                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

//            // Act
//            var result = await _controller.CompleteRegistration(dto);

//            // Assert -> expected BadRequest because ResetPassword failed
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }
//    }
//}
