using System.Linq.Expressions;
using System.Security.Claims;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Queries.Identity;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using MockQueryable;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Identity;

public class GetMeHandlerTests
{
    private readonly GetMeHandler _handler;
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;

    public GetMeHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>>();
        _handler = new GetMeHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnUserInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Users
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var users = new List<Users> { user }.AsQueryable().BuildMock();
        _userRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .Returns(users);

        var query = new Query.GetMe(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Email.Should().Be("test@example.com");
        response.Username.Should().Be("testuser");
        response.Firstname.Should().Be("John");
        response.Lastname.Should().Be("Doe");
        response.PhoneNumber.Should().Be("123456789");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new List<Users>().AsQueryable().BuildMock();
        _userRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .Returns(users);

        var query = new Query.GetMe(userId);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
    }
}

public class GetUsersQueryHandlerTests
{
    private readonly GetUsersQueryHandler _handler;
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;

    public GetUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>>();
        _handler = new GetUsersQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnUsersPage()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var users = new List<Users>
        {
            new()
            {
                Id = userId1,
                Email = "user1@example.com",
                Username = "user1",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "123456789",
                IsDeleted = false,
                Roles = new Roles { Name = "Customer" },
                Factories = new Factories { Id = Guid.NewGuid() },
                CreatedOnUtc = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = userId2,
                Email = "user2@example.com",
                Username = "user2",
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "987654321",
                IsDeleted = false,
                Roles = new Roles { Name = "Factory" },
                Factories = new Factories { Id = Guid.NewGuid() },
                CreatedOnUtc = DateTimeOffset.UtcNow
            }
        };

        var query = users.AsQueryable().BuildMock();
        _userRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .Returns(query);

        var request = new Query.GetUsers(null, 1, 10);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Email.Should().Be("user1@example.com");
        result.Value.Items[1].Email.Should().Be("user2@example.com");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnFilteredUsers()
    {
        // Arrange
        var users = new List<Users>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "john@example.com",
                Username = "john",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "123456789",
                IsDeleted = false,
                Roles = new Roles { Name = "Customer" },
                Factories = new Factories { Id = Guid.NewGuid() },
                CreatedOnUtc = DateTimeOffset.UtcNow
            }
        };

        var filteredUsers = users.Where(u => u.Email.Contains("john") || u.Username.Contains("john")).AsQueryable()
            .BuildMock();
        _userRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .Returns(filteredUsers);

        var request = new Query.GetUsers("john", 1, 10);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Email.Should().Be("john@example.com");
    }
}

public class GetLoginQueryHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetLoginQueryHandler _handler;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;

    public GetLoginQueryHandlerTests()
    {
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>>();
        _passwordHasherServiceMock = new Mock<IPasswordHasherService>();

        _handler = new GetLoginQueryHandler(
            _jwtTokenServiceMock.Object,
            _cacheServiceMock.Object,
            _userRepositoryMock.Object,
            _passwordHasherServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldLoginSuccessfully()
    {
        // Arrange
        var query = new Query.Login("test@example.com", "Password123");

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var hashedPassword = "hashedPassword123";

        var user = new Users
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "1234567890",
            Password = hashedPassword,
            Roles = new Roles { Id = roleId, Name = "Customer" },
            CreatedOnUtc = DateTimeOffset.UtcNow.AddDays(-10)
        };

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _passwordHasherServiceMock
            .Setup(x => x.VerifyPassword(query.Password, hashedPassword))
            .Returns(true);

        var accessToken = "generated-access-token";
        var refreshToken = "generated-refresh-token";

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(accessToken);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(accessToken, result.Value.AccessToken);
        Assert.Equal(refreshToken, result.Value.RefreshToken);
        Assert.NotNull(result.Value.User);
        Assert.Equal(userId, result.Value.User.Id);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(user.Username, result.Value.User.Username);
        Assert.Equal(user.FirstName, result.Value.User.Firstname);
        Assert.Equal(user.LastName, result.Value.User.Lastname);
        Assert.Equal(user.PhoneNumber, result.Value.User.PhoneNumber);
        Assert.Equal(user.Roles.Name, result.Value.User.RoleName);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"{nameof(Query.Login)}-UserAccount:{query.EmailOrUserName}",
                It.IsAny<Response.Authenticated>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var query = new Query.Login("nonexistent@example.com", "Password123");

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync((Users)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
        Assert.Equal("User Not Existed !", exception.Message);

        _passwordHasherServiceMock.Verify(
            x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithIncorrectPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var query = new Query.Login("test@example.com", "WrongPassword");

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var hashedPassword = "hashedPassword123";

        var user = new Users
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Password = hashedPassword,
            Roles = new Roles { Id = roleId, Name = "Customer" }
        };

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _passwordHasherServiceMock
            .Setup(x => x.VerifyPassword(query.Password, hashedPassword))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.Equal("UnAuthorize !", exception.Message);

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()),
            Times.Never);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithFactoryUser_ShouldIncludeFactoryIdInClaims()
    {
        // Arrange
        var query = new Query.Login("factory@example.com", "Password123");

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var factoryId = Guid.NewGuid();
        var hashedPassword = "hashedPassword123";

        var user = new Users
        {
            Id = userId,
            Email = "factory@example.com",
            Username = "factoryuser",
            FirstName = "Factory",
            LastName = "Owner",
            PhoneNumber = "1234567890",
            Password = hashedPassword,
            Roles = new Roles { Id = roleId, Name = "Factory" },
            FactoriesId = factoryId,
            Factories = new Factories { Id = factoryId },
            CreatedOnUtc = DateTimeOffset.UtcNow.AddDays(-10)
        };

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _passwordHasherServiceMock
            .Setup(x => x.VerifyPassword(query.Password, hashedPassword))
            .Returns(true);

        var accessToken = "generated-access-token";
        var refreshToken = "generated-refresh-token";

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(accessToken);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        IEnumerable<Claim> capturedClaims = null;
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Callback<IEnumerable<Claim>>(claims => capturedClaims = claims)
            .Returns(accessToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedClaims);
        Assert.Contains(capturedClaims, claim =>
            claim.Type == "FactoryId" && claim.Value == factoryId.ToString());
        Assert.Equal(factoryId, result.Value.User.FactoryId);
    }
}

public class GetTokenQueryHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetTokenQueryHandler _handler;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;

    public GetTokenQueryHandlerTests()
    {
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _cacheServiceMock = new Mock<ICacheService>();

        _handler = new GetTokenQueryHandler(
            _jwtTokenServiceMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidExpiredToken_ShouldReturnNewAccessToken()
    {
        // Arrange
        var accessToken = "expired-access-token";
        var refreshToken = "valid-refresh-token";
        var query = new Query.Token(accessToken, refreshToken);

        var userAccount = "test@example.com";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userAccount)
        }, "test"));

        _jwtTokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns((claimsPrincipal, true)); // Token is expired

        var cachedResponse = new Response.Authenticated
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTimeOffset.Now.AddMinutes(30)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<Response.Authenticated>(
                $"{nameof(Query.Login)}-UserAccount:{userAccount}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var newAccessToken = "new-access-token";

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(newAccessToken);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newAccessToken, result.Value.AccessToken);
        Assert.Equal(refreshToken, result.Value.RefreshToken);

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"{nameof(Query.Login)}-UserAccount:{userAccount}",
                It.IsAny<Response.Authenticated>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidNonExpiredToken_ShouldReturnCachedResponse()
    {
        // Arrange
        var accessToken = "valid-access-token";
        var refreshToken = "valid-refresh-token";
        var query = new Query.Token(accessToken, refreshToken);

        var userAccount = "test@example.com";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userAccount)
        }, "test"));

        _jwtTokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns((claimsPrincipal, false)); // Token is not expired

        var cachedResponse = new Response.Authenticated
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTimeOffset.Now.AddMinutes(30)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<Response.Authenticated>(
                $"{nameof(Query.Login)}-UserAccount:{userAccount}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accessToken, result.Value.AccessToken); // Should return the same access token
        Assert.Equal(refreshToken, result.Value.RefreshToken);

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()),
            Times.Never); // Should never generate a new token

        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<Response.Authenticated>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never); // Should never set a new cached response
    }

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ShouldThrowException()
    {
        // Arrange
        var accessToken = "access-token";
        var refreshToken = "invalid-refresh-token";
        var query = new Query.Token(accessToken, refreshToken);

        var userAccount = "test@example.com";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userAccount)
        }, "test"));

        _jwtTokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns((claimsPrincipal, true));

        var cachedResponse = new Response.Authenticated
        {
            AccessToken = accessToken,
            RefreshToken = "different-refresh-token", // Different from the one in the request
            RefreshTokenExpiryTime = DateTimeOffset.Now.AddMinutes(30)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<Response.Authenticated>(
                $"{nameof(Query.Login)}-UserAccount:{userAccount}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);

        _jwtTokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingCachedResponse_ShouldThrowException()
    {
        // Arrange
        var accessToken = "access-token";
        var refreshToken = "refresh-token";
        var query = new Query.Token(accessToken, refreshToken);

        var userAccount = "test@example.com";
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, userAccount)
        }, "test"));

        _jwtTokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns((claimsPrincipal, true));

        _cacheServiceMock
            .Setup(x => x.GetAsync<Response.Authenticated>(
                $"{nameof(Query.Login)}-UserAccount:{userAccount}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Response.Authenticated)null); // No cached response

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldThrowExceptionFromJwtService()
    {
        // Arrange
        var accessToken = "invalid-access-token";
        var refreshToken = "refresh-token";
        var query = new Query.Token(accessToken, refreshToken);

        var expectedException = new Exception("Token validation failed");

        _jwtTokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Throws(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.Same(expectedException, exception);

        _cacheServiceMock.Verify(
            x => x.GetAsync<Response.Authenticated>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}