using System.Linq.Expressions;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Commands.Identity;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Commands.Identity;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Customers, Guid>> _customerRepositoryMock;
    private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>>();
        _customerRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Customers, Guid>>();
        _passwordHasherServiceMock = new Mock<IPasswordHasherService>();
        
        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object, 
            _passwordHasherServiceMock.Object, 
            _customerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCustomerRegistration_ShouldSucceed()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "test@example.com",
            "testuser",
            "password123",
            "John",
            "Doe",
            "1234567890",
            0 // Customer role
        );

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<Expression<Func<Users, object>>[]>()))!
            .ReturnsAsync((Users)null); // No existing userxisting user

        _passwordHasherServiceMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.Add(It.IsAny<Users>()), Times.Once);
        _customerRepositoryMock.Verify(x => x.Add(It.IsAny<Customers>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldThrowException()
    {
        // Arrange
        var command = new Command.RegisterCommand(
            "existing@example.com",
            "existinguser",
            "password123",
            "John",
            "Doe",
            "1234567890",
            0
        );

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<System.Linq.Expressions.Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(new Users()); // Existing user

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _handler.Handle(command, CancellationToken.None));
    }
}

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
        result.Error.Should().Be(Error.None);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync($"{nameof(Query.Login)}-UserAccount:{command.UserAccount}", 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}