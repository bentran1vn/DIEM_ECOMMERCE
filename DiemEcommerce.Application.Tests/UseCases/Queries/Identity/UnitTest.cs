using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Queries.Identity;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using MockQueryable;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Identity;

public class GetMeHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;
    private readonly GetMeHandler _handler;

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
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(), It.IsAny<Expression<Func<Users, object>>[]>()))
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
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(), It.IsAny<Expression<Func<Users, object>>[]>()))
            .Returns(users);

        var query = new Query.GetMe(userId);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
    }
}

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Users, Guid>> _userRepositoryMock;
    private readonly GetUsersQueryHandler _handler;

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
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(), It.IsAny<Expression<Func<Users, object>>[]>()))
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

        var filteredUsers = users.Where(u => u.Email.Contains("john") || u.Username.Contains("john")).AsQueryable().BuildMock();
        _userRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Users, bool>>>(), It.IsAny<Expression<Func<Users, object>>[]>()))
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