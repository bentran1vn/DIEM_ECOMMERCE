using System.Linq.Expressions;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Commands.Identity;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
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
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;
    private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly ChangePasswordCommandHandler _handler;

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
    private readonly Mock<IMailService> _mailServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;
    private readonly ForgotPasswordCommandHandler _handler;

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
                It.IsAny<Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions>(),
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