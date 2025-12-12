//using System;
//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using CRM.Server.Controllers;
//using CRM.Server.DTOs.Users;
//using CRM.Server.Services.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using Xunit;
//using FluentAssertions;

//namespace CRM.Server.Tests.Controllers
//{
//    public class UserControllerTests
//    {
//        private readonly Mock<IUserService> _serviceMock;
//        private readonly UserController _controller;

//        public UserControllerTests()
//        {
//            _serviceMock = new Mock<IUserService>();
//            _controller = new UserController(_serviceMock.Object);
//            // Ensure controller has a default ControllerContext so we can set HttpContext later
//            _controller.ControllerContext = new ControllerContext
//            {
//                HttpContext = new DefaultHttpContext()
//            };
//        }

//        // -------------------------------------------------------
//        // CREATE USER - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task CreateUser_Should_Return_Ok_With_Success_When_Service_Succeeds()
//        {
//            // Arrange
//            var dto = new CreateUserDto
//            {
//                FullName = "John Doe",
//                Email = "john@example.com",
//                Role = "User"
//            };

//            _serviceMock
//                .Setup(s => s.CreateUserAsync(It.Is<CreateUserDto>(d => d.Email == dto.Email)))
//                .Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.CreateUser(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().NotBeNull();

//            var successProp = ok.Value.GetType().GetProperty("success");
//            var messageProp = ok.Value.GetType().GetProperty("message");

//            successProp.Should().NotBeNull();
//            messageProp.Should().NotBeNull();

//            var successVal = (bool)successProp!.GetValue(ok.Value)!;
//            var messageVal = (string)messageProp!.GetValue(ok.Value)!;

//            successVal.Should().BeTrue();
//            messageVal.Should().Be("User created successfully");
//        }

//        // -------------------------------------------------------
//        // CREATE USER - FAILURE (service throws) => Controller returns Ok(success=false)
//        // -------------------------------------------------------
//        [Fact]
//        public async Task CreateUser_Should_Return_Ok_With_SuccessFalse_When_Service_Throws()
//        {
//            // Arrange
//            var dto = new CreateUserDto { FullName = "J", Email = "exists@example.com", Role = "User" };

//            _serviceMock
//                .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserDto>()))
//                .ThrowsAsync(new Exception("Email already exists"));

//            // Act
//            var result = await _controller.CreateUser(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().NotBeNull();

//            var successProp = ok.Value.GetType().GetProperty("success");
//            var messageProp = ok.Value.GetType().GetProperty("message");

//            successProp.Should().NotBeNull();
//            messageProp.Should().NotBeNull();

//            var successVal = (bool)successProp!.GetValue(ok.Value)!;
//            var messageVal = (string)messageProp!.GetValue(ok.Value)!;

//            successVal.Should().BeFalse();
//            messageVal.Should().Be("Email already exists");
//        }

//        // -------------------------------------------------------
//        // INVITE USER - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task InviteUser_Should_Return_Ok_With_Success_When_Service_Succeeds()
//        {
//            // Arrange
//            var dto = new InviteUserDto { FullName = "Invite", Email = "invite@example.com", Role = "User" };

//            _serviceMock
//                .Setup(s => s.InviteUserAsync(It.Is<InviteUserDto>(d => d.Email == dto.Email)))
//                .Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.InviteUser(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().NotBeNull();

//            var successVal = (bool)ok.Value.GetType().GetProperty("success")!.GetValue(ok.Value)!;
//            var messageVal = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;

//            successVal.Should().BeTrue();
//            messageVal.Should().Be("Invitation sent successfully");
//        }

//        // -------------------------------------------------------
//        // INVITE USER - FAILURE -> Ok(success=false)
//        // -------------------------------------------------------
//        [Fact]
//        public async Task InviteUser_Should_Return_Ok_With_SuccessFalse_When_Service_Throws()
//        {
//            // Arrange
//            var dto = new InviteUserDto { FullName = "Invite", Email = "exists@example.com", Role = "User" };

//            _serviceMock
//                .Setup(s => s.InviteUserAsync(It.IsAny<InviteUserDto>()))
//                .ThrowsAsync(new Exception("Email already exists"));

//            // Act
//            var result = await _controller.InviteUser(dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);

//            var successVal = (bool)ok.Value.GetType().GetProperty("success")!.GetValue(ok.Value)!;
//            var messageVal = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;

//            successVal.Should().BeFalse();
//            messageVal.Should().Be("Email already exists");
//        }

//        // -------------------------------------------------------
//        // GET ALL USERS - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task GetAllUsers_Should_Return_Ok_With_List()
//        {
//            // Arrange
//            var users = new List<UserResponseDto>
//            {
//                new UserResponseDto { Id = "1", FullName = "A", Email = "a@e.com", Role = "User", IsActive = true, TwoFactorEnabled = false },
//                new UserResponseDto { Id = "2", FullName = "B", Email = "b@e.com", Role = "Admin", IsActive = true, TwoFactorEnabled = true },
//            };

//            _serviceMock
//                .Setup(s => s.GetAllUsersAsync())
//                .ReturnsAsync(users);

//            // Act
//            var result = await _controller.GetAllUsers();

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(users);
//        }

//        // -------------------------------------------------------
//        // GET ALL USERS - FAILURE (service throws) -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task GetAllUsers_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            _serviceMock
//                .Setup(s => s.GetAllUsersAsync())
//                .ThrowsAsync(new Exception("Database error"));

//            // Act
//            var result = await _controller.GetAllUsers();

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // DEACTIVATE - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Deactivate_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var userId = "user-1";
//            _serviceMock.Setup(s => s.DeactivateUserAsync(userId)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.Deactivate(userId);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var message = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;
//            message.Should().Be("User deactivated successfully");
//        }

//        // -------------------------------------------------------
//        // DEACTIVATE - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Deactivate_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            var userId = "user-1";
//            _serviceMock.Setup(s => s.DeactivateUserAsync(userId)).ThrowsAsync(new Exception("User not found"));

//            // Act
//            var result = await _controller.Deactivate(userId);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // ACTIVATE - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Activate_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var userId = "user-activate";
//            _serviceMock.Setup(s => s.ActivateUserAsync(userId)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.Activate(userId);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var message = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;
//            message.Should().Be("User activated successfully");
//        }

//        // -------------------------------------------------------
//        // ACTIVATE - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Activate_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            var userId = "user-activate";
//            _serviceMock.Setup(s => s.ActivateUserAsync(userId)).ThrowsAsync(new Exception("User not found"));

//            // Act
//            var result = await _controller.Activate(userId);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // UPDATE - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task UpdateUser_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var userId = "u1";
//            var dto = new UpdateUserDto { FullName = "New", Email = "n@e.com", Role = "User", IsActive = true };

//            _serviceMock.Setup(s => s.UpdateUserAsync(userId, It.Is<UpdateUserDto>(d => d.Email == dto.Email)))
//                        .Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.UpdateUser(userId, dto);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var message = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;
//            message.Should().Be("User updated successfully");
//        }

//        // -------------------------------------------------------
//        // UPDATE - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task UpdateUser_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            var userId = "u1";
//            var dto = new UpdateUserDto { FullName = "New", Email = "n@e.com", Role = "User" };

//            _serviceMock.Setup(s => s.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>()))
//                        .ThrowsAsync(new Exception("User not found"));

//            // Act
//            var result = await _controller.UpdateUser(userId, dto);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // DELETE - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task DeleteUser_Should_Return_Ok_On_Success()
//        {
//            // Arrange
//            var userId = "delete-1";
//            _serviceMock.Setup(s => s.DeleteUserAsync(userId)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.DeleteUser(userId);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var message = (string)ok.Value.GetType().GetProperty("message")!.GetValue(ok.Value)!;
//            message.Should().Be("User deleted successfully");
//        }

//        // -------------------------------------------------------
//        // DELETE - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task DeleteUser_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            var userId = "delete-1";
//            _serviceMock.Setup(s => s.DeleteUserAsync(userId)).ThrowsAsync(new Exception("User not found"));

//            // Act
//            var result = await _controller.DeleteUser(userId);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // GET BY ID - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task GetUserById_Should_Return_Ok_With_User()
//        {
//            // Arrange
//            var userId = "u123";
//            var user = new UserResponseDto { Id = userId, FullName = "User", Email = "u@e.com", Role = "User", IsActive = true, TwoFactorEnabled = false };

//            _serviceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

//            // Act
//            var result = await _controller.GetUserById(userId);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(user);
//        }

//        // -------------------------------------------------------
//        // GET BY ID - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task GetUserById_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            var userId = "notfound";
//            _serviceMock.Setup(s => s.GetUserByIdAsync(userId)).ThrowsAsync(new Exception("User not found"));

//            // Act
//            var result = await _controller.GetUserById(userId);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // GET MY PROFILE - SUCCESS (authenticated user)
//        // -------------------------------------------------------
//        [Fact]
//        public async Task GetMyProfile_Should_Return_Ok_With_User_For_Authenticated_User()
//        {
//            // Arrange
//            var userId = "me-1";
//            var user = new UserResponseDto { Id = userId, FullName = "Me", Email = "me@e.com", Role = "User", IsActive = true, TwoFactorEnabled = false };

//            _serviceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

//            // Setup authenticated user in HttpContext
//            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, userId)
//            }, "test"));

//            _controller.ControllerContext.HttpContext.User = claims;

//            // Act
//            var result = await _controller.GetMyProfile();

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(user);
//        }

//        // -------------------------------------------------------
//        // FILTER USERS - SUCCESS
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Filter_Should_Return_Ok_With_Filtered_Users()
//        {
//            // Arrange
//            var role = "User";
//            bool? isActive = true;

//            var list = new List<UserResponseDto>
//            {
//                new UserResponseDto { Id = "1", FullName = "A", Email = "a@e.com", Role = "User", IsActive = true, TwoFactorEnabled = false }
//            };

//            _serviceMock.Setup(s => s.FilterUsersAsync(role, isActive)).ReturnsAsync(list);

//            // Act
//            var result = await _controller.Filter(role, isActive);

//            // Assert
//            var ok = Assert.IsType<OkObjectResult>(result);
//            ok.Value.Should().BeEquivalentTo(list);
//        }

//        // -------------------------------------------------------
//        // FILTER USERS - FAILURE -> BadRequest
//        // -------------------------------------------------------
//        [Fact]
//        public async Task Filter_Should_Return_BadRequest_When_Service_Throws()
//        {
//            // Arrange
//            _serviceMock.Setup(s => s.FilterUsersAsync(It.IsAny<string?>(), It.IsAny<bool?>()))
//                        .ThrowsAsync(new Exception("DB error"));

//            // Act
//            var result = await _controller.Filter(null, null);

//            // Assert
//            var bad = Assert.IsType<BadRequestObjectResult>(result);
//            bad.Value.Should().NotBeNull();
//        }

//        // -------------------------------------------------------
//        // PRESENTATION: Meaningful failing test
//        // Demonstrates that CreateUser rejects duplicate emails
//        // -------------------------------------------------------
//        [Fact]
//        public async Task CreateUser_PresentationFail_DuplicateEmail_Shows_ExpectedFailure()
//        {
//            // Arrange - service throws duplicate email
//            var dto = new CreateUserDto { FullName = "X", Email = "dup@example.com", Role = "User" };

//            _serviceMock.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserDto>()))
//                        .ThrowsAsync(new Exception("Email already exists"));

//            // Act
//            var result = await _controller.CreateUser(dto);

//            // Assert - intentionally expect success true to show failure during presentation
//            var ok = Assert.IsType<OkObjectResult>(result);
//            var successVal = (bool)ok.Value.GetType().GetProperty("success")!.GetValue(ok.Value)!;

//            // This will FAIL: successVal is false. Good for presentation to demonstrate failure.
//            successVal.Should().BeTrue("we intentionally assert the wrong expectation to demonstrate a failing test case");
//        }
//    }
//}
