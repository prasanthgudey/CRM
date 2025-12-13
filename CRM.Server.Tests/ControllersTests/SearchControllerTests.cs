using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using CRM.Server.Controllers;
using CRM.Server.DTOs.Shared;
using CRM.Server.Services.Interfaces;

namespace CRM.Server.Tests.Controllers
{
    public class SearchControllerTests
    {
        private Mock<ISearchService> _searchServiceMock = new();

        private SearchController CreateController(ClaimsPrincipal? user = null)
        {
            var controller = new SearchController(_searchServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            if (user != null) httpContext.User = user;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }

        private static ClaimsPrincipal MakeUser(string userId = "user-1")
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "Test");
            return new ClaimsPrincipal(identity);
        }

        private static SearchResultDto MakeResult(string id, string title)
            => new SearchResultDto
            {
                EntityType = "Customer",
                Id = id,
                Title = title,
                Subtitle = "sub",
                Snippet = "snippet",
                Route = $"/customers/{id}",
                Date = DateTime.UtcNow
            };

        [Fact]
        public async Task Search_WhenServiceReturnsResults_ReturnsOkWithSameResults()
        {
            // Arrange
            var user = MakeUser();
            var controller = CreateController(user);

            var results = new List<SearchResultDto>
            {
                MakeResult("1", "Alpha"),
                MakeResult("2", "Beta")
            };

            _searchServiceMock
                .Setup(s => s.SearchAsync("query", 25, user))
                .ReturnsAsync(results);

            // Act
            var actionResult = await controller.Search("query");

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var ok = actionResult as OkObjectResult;
            ok!.Value.Should().BeAssignableTo<List<SearchResultDto>>();
            var returned = ok.Value as List<SearchResultDto>;
            returned.Should().HaveCount(2);
            returned![0].Id.Should().Be("1");

            _searchServiceMock.Verify(s => s.SearchAsync("query", 25, user), Times.Once);
        }

        [Fact]
        public async Task Search_WithCustomLimit_ForwardsLimitToService()
        {
            // Arrange
            var user = MakeUser();
            var controller = CreateController(user);

            var results = new List<SearchResultDto>();
            _searchServiceMock
                .Setup(s => s.SearchAsync("foo", 5, user))
                .ReturnsAsync(results);

            // Act
            var actionResult = await controller.Search("foo", limit: 5);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            _searchServiceMock.Verify(s => s.SearchAsync("foo", 5, user), Times.Once);
        }

        [Fact]
        public async Task Search_WhenServiceReturnsEmpty_ReturnsOkWithEmptyList()
        {
            // Arrange
            var user = MakeUser();
            var controller = CreateController(user);

            _searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ClaimsPrincipal>()))
                              .ReturnsAsync(new List<SearchResultDto>());

            // Act
            var actionResult = await controller.Search("nothing");

            // Assert
            var ok = actionResult as OkObjectResult;
            ok!.Value.Should().BeAssignableTo<List<SearchResultDto>>();
            var returned = ok.Value as List<SearchResultDto>;
            returned.Should().BeEmpty();
            _searchServiceMock.Verify(s => s.SearchAsync("nothing", 25, user), Times.Once);
        }

        [Fact]
        public async Task Search_WhenServiceThrows_ExceptionPropagates()
        {
            // Arrange
            var user = MakeUser();
            var controller = CreateController(user);

            _searchServiceMock
                .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ClaimsPrincipal>()))
                .ThrowsAsync(new InvalidOperationException("search failed"));

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Search("bad"));
            _searchServiceMock.Verify(s => s.SearchAsync("bad", 25, user), Times.Once);
        }
    }
}