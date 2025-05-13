using System.Linq.Expressions;
using System.Security.Claims;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Commands.Identity;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Commands.Identity;

public class LogoutCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _handler = new LogoutCommandHandler(_cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldRemoveCacheAndReturnSuccess()
    {
        // Arrange
        var command = new Command.LogoutCommand("test@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // result.Error.Message.Should().Be("Logout Successfully");
        _cacheServiceMock.Verify(
            x => x.RemoveAsync($"{nameof(Query.Login)}-UserAccount:{command.UserAccount}",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly ChangePasswordCommandHandler _handler;
    private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;

    public ChangePasswordCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>>();
        _passwordHasherServiceMock = new Mock<IPasswordHasherService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _handler = new ChangePasswordCommandHandler(
            _passwordHasherServiceMock.Object,
            _cacheServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUser_ShouldChangePassword()
    {
        // Arrange
        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "oldHashedPassword"
        };
        var command = new Command.ChangePasswordCommand("test@example.com", "newPassword");

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _passwordHasherServiceMock
            .Setup(x => x.HashPassword("newPassword"))
            .Returns("newHashedPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // result.Error.Message.Should().Be("Change Password Successfully !");
        user.Password.Should().Be("newHashedPassword");
        _cacheServiceMock.Verify(
            x => x.RemoveAsync($"{nameof(Query.Login)}-UserAccount:{user.Email}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var command = new Command.ChangePasswordCommand("nonexistent@example.com", "newPassword");

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))!
            .ReturnsAsync((Users)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
    }
}

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly ForgotPasswordCommandHandler _handler;
    private readonly Mock<IMailService> _mailServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;

    public ForgotPasswordCommandHandlerTests()
    {
        _mailServiceMock = new Mock<IMailService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>>();
        _handler = new ForgotPasswordCommandHandler(
            _mailServiceMock.Object,
            _cacheServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUser_ShouldSendEmailAndSetCache()
    {
        // Arrange
        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var command = new Command.ForgotPasswordCommand("test@example.com");

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // result.Error.Message.Should().Be("Send Mail Successfully !");
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"{nameof(Command.ForgotPasswordCommand)}-UserAccount:{user.Email}",
                It.IsAny<string>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var command = new Command.ForgotPasswordCommand("nonexistent@example.com");

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))!
            .ReturnsAsync((Users)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
    }
}

public class RegisterCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Customers, Guid>> _customersRepositoryMock;
    private readonly RegisterCommandHandler _handler;
    private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>>();
        _customersRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Customers, Guid>>();
        _passwordHasherServiceMock = new Mock<IPasswordHasherService>();

        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherServiceMock.Object,
            _customersRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewCustomerUser_ShouldRegisterSuccessfully()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "test@example.com",
            "testuser",
            "Password123",
            "John",
            "Doe",
            "1234567890",
            0 // Customer role
        );

        var hashedPassword = "hashedPassword123";

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync((Users)null);

        _passwordHasherServiceMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        Users capturedUser = null;
        Customers capturedCustomer = null;

        _userRepositoryMock
            .Setup(x => x.Add(It.IsAny<Users>()))
            .Callback<Users>(user => capturedUser = user);

        _customersRepositoryMock
            .Setup(x => x.Add(It.IsAny<Customers>()))
            .Callback<Customers>(customer => capturedCustomer = customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.Add(It.IsAny<Users>()), Times.Once);
        _customersRepositoryMock.Verify(x => x.Add(It.IsAny<Customers>()), Times.Once);

        Assert.NotNull(capturedUser);
        Assert.NotNull(capturedCustomer);

        Assert.Equal(command.Email, capturedUser.Email);
        Assert.Equal(command.Username, capturedUser.Username);
        Assert.Equal(command.FirstName, capturedUser.FirstName);
        Assert.Equal(command.LastName, capturedUser.LastName);
        Assert.Equal(command.Phonenumber, capturedUser.PhoneNumber);
        Assert.Equal(hashedPassword, capturedUser.Password);
        Assert.Equal(new Guid("5a900888-430b-4073-a2f4-824659ff36bf"), capturedUser.RolesId); // Customer role ID
        Assert.Equal(capturedCustomer.Id, capturedUser.CustomersId);
    }

    [Fact]
    public async Task Handle_WithNewFactoryUser_ShouldRegisterSuccessfully()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "factory@example.com",
            "factoryuser",
            "Factory123",
            "Factory",
            "Owner",
            "9876543210",
            1 // Factory role
        );

        var hashedPassword = "hashedFactoryPassword";

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync((Users)null);

        _passwordHasherServiceMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns(hashedPassword);

        Users capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.Add(It.IsAny<Users>()))
            .Callback<Users>(user => capturedUser = user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.Add(It.IsAny<Users>()), Times.Once);
        _customersRepositoryMock.Verify(x => x.Add(It.IsAny<Customers>()), Times.Never);

        Assert.NotNull(capturedUser);

        Assert.Equal(command.Email, capturedUser.Email);
        Assert.Equal(command.Username, capturedUser.Username);
        Assert.Equal(command.FirstName, capturedUser.FirstName);
        Assert.Equal(command.LastName, capturedUser.LastName);
        Assert.Equal(command.Phonenumber, capturedUser.PhoneNumber);
        Assert.Equal(hashedPassword, capturedUser.Password);
        Assert.Equal(new Guid("6a900888-430b-4073-a2f4-824659ff36bf"), capturedUser.RolesId); // Factory role ID
        Assert.Null(capturedUser.CustomersId);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldThrowException()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "existing@example.com",
            "existinguser",
            "Password123",
            "Existing",
            "User",
            "1234567890",
            0
        );

        var existingUser = new Users
        {
            Email = command.Email,
            Username = command.Username
        };

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("User Existed !", exception.Message);

        _userRepositoryMock.Verify(x => x.Add(It.IsAny<Users>()), Times.Never);
        _customersRepositoryMock.Verify(x => x.Add(It.IsAny<Customers>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldThrowException()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "test@example.com",
            "testuser",
            "Password123",
            "John",
            "Doe",
            "1234567890",
            2 // Invalid role
        );

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync((Users)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Role not found !", exception.Message);

        _userRepositoryMock.Verify(x => x.Add(It.IsAny<Users>()), Times.Never);
        _customersRepositoryMock.Verify(x => x.Add(It.IsAny<Customers>()), Times.Never);
    }
}

public class VerifyCodeCommandHandlerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;
    private readonly VerifyCodeCommandHandler _handler;

    public VerifyCodeCommandHandlerTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>>();
        
        _handler = new VerifyCodeCommandHandler(
            _cacheServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCode_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var verificationCode = "123456";
        var userName = "bentran1vn";
        
        var command = new Command.VerifyCodeCommand(userEmail, verificationCode);
        
        var user = new Users
        {
            Id = userId,
            Email = userEmail,
            Roles = new Roles { Id = Guid.NewGuid(), Name = "Customer" },
            Username = userName
        };
        
        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);
            
        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                $"{nameof(Command.ForgotPasswordCommand)}-UserAccount:{userEmail}", 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationCode);
            
        var accessToken = "generated-access-token";
        var refreshToken = "generated-refresh-token";
        
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns(accessToken);
            
        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                $"{nameof(Query.Login)}-UserAccount:{userEmail}",
                It.IsAny<Response.Authenticated>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var userEmail = "test@example.com";
        var correctCode = "123456";
        var wrongCode = "654321";
        
        var command = new Command.VerifyCodeCommand(userEmail, wrongCode);
        
        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            Roles = new Roles { Id = Guid.NewGuid(), Name = "Customer" }
        };
        
        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);
            
        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                $"{nameof(Command.ForgotPasswordCommand)}-UserAccount:{userEmail}", 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(correctCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
        Assert.Equal("Verify Code is Wrong !", result.Error.Message);
        
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
    public async Task Handle_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userEmail = "nonexistent@example.com";
        var verificationCode = "123456";
        
        var command = new Command.VerifyCodeCommand(userEmail, verificationCode);
        
        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync((Users)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("User Not Existed !", exception.Message);
        
        _cacheServiceMock.Verify(
            x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNoCodeInCache_ShouldReturnFailure()
    {
        // Arrange
        var userEmail = "test@example.com";
        var verificationCode = "123456";
        
        var command = new Command.VerifyCodeCommand(userEmail, verificationCode);
        
        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = userEmail,
            Roles = new Roles { Id = Guid.NewGuid(), Name = "Customer" }
        };
        
        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);
            
        _cacheServiceMock
            .Setup(x => x.GetAsync<string>(
                $"{nameof(Command.ForgotPasswordCommand)}-UserAccount:{userEmail}", 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
        Assert.Equal("Verify Code is Wrong !", result.Error.Message);
    }
}

