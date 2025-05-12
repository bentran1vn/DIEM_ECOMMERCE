using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Queries.Factory;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using MockQueryable;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Factory;

public class GetAllFactoriesQueryHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid>> _factoryRepositoryMock;
    private readonly GetAllFactoriesQueryHandler _handler;

    public GetAllFactoriesQueryHandlerTests()
    {
        _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid>>();
        _handler = new GetAllFactoriesQueryHandler(_factoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ShouldReturnFactoriesPage()
    {
        // Arrange
        var factoryId1 = Guid.NewGuid();
        var factoryId2 = Guid.NewGuid();

        var factories = new List<Factories>
        {
            new() 
            { 
                Id = factoryId1,
                Name = "Factory 1",
                Address = "Address 1",
                PhoneNumber = "123456789",
                Email = "factory1@example.com",
                Website = "http://factory1.com",
                Description = "Description 1",
                Logo = "logo1.jpg",
                TaxCode = "TAX001",
                BankAccount = "BANK001",
                BankName = "Bank 1",
                IsDeleted = false,
                CreatedOnUtc = DateTimeOffset.UtcNow
            },
            new() 
            { 
                Id = factoryId2,
                Name = "Factory 2",
                Address = "Address 2",
                PhoneNumber = "987654321",
                Email = "factory2@example.com",
                Website = "http://factory2.com",
                Description = "Description 2",
                Logo = "logo2.jpg",
                TaxCode = "TAX002",
                BankAccount = "BANK002",
                BankName = "Bank 2",
                IsDeleted = false,
                CreatedOnUtc = DateTimeOffset.UtcNow
            }
        };

        var query = factories.AsQueryable().BuildMock();
        _factoryRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Factories, bool>>>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .Returns(query);

        var request = new Contract.Services.Factory.Queries.GetAllFactoriesQuery(1, 10, null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Name.Should().Be("Factory 1");
        result.Value.Items[1].Name.Should().Be("Factory 2");
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnFilteredFactories()
    {
        // Arrange
        var factories = new List<Factories>
        {
            new() 
            { 
                Id = Guid.NewGuid(),
                Name = "Electronics Factory",
                Description = "Electronic device manufacturing",
                IsDeleted = false,
                CreatedOnUtc = DateTimeOffset.UtcNow
            },
            new() 
            { 
                Id = Guid.NewGuid(),
                Name = "Clothing Factory",
                Description = "Textile and clothing production",
                IsDeleted = false,
                CreatedOnUtc = DateTimeOffset.UtcNow
            }
        };

        var filteredFactories = factories.Where(f => 
            f.Name.ToLower().Contains("electronics") || 
            f.Description.ToLower().Contains("electronics")).AsQueryable().BuildMock();
        
        _factoryRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Factories, bool>>>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .Returns(filteredFactories);

        var request = new Contract.Services.Factory.Queries.GetAllFactoriesQuery(1, 10, "electronics");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Electronics Factory");
    }

    [Fact]
    public async Task Handle_WithNoFactories_ShouldReturnEmptyPage()
    {
        // Arrange
        var factories = new List<Factories>();
        var query = factories.AsQueryable().BuildMock();
        
        _factoryRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Factories, bool>>>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .Returns(query);

        var request = new Contract.Services.Factory.Queries.GetAllFactoriesQuery(1, 10, null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}

public class GetFactoryByIdQueryHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid>> _factoryRepositoryMock;
    private readonly GetFactoryByIdQueryHandler _handler;

    public GetFactoryByIdQueryHandlerTests()
    {
        _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid>>();
        _handler = new GetFactoryByIdQueryHandler(_factoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnFactory()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var factory = new Factories
        {
            Id = factoryId,
            Name = "Test Factory",
            Address = "Test Address",
            PhoneNumber = "123456789",
            Email = "factory@example.com",
            Website = "http://factory.com",
            Description = "Test Description",
            Logo = "logo.jpg",
            TaxCode = "TAX123",
            BankAccount = "BANK123",
            BankName = "Test Bank",
            IsDeleted = false,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(factory);

        var request = new Contract.Services.Factory.Queries.GetFactoryByIdQuery(factoryId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Id.Should().Be(factoryId);
        response.Name.Should().Be("Test Factory");
        response.Email.Should().Be("factory@example.com");
        response.TaxCode.Should().Be("TAX123");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var factoryId = Guid.NewGuid();

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        var request = new Contract.Services.Factory.Queries.GetFactoryByIdQuery(factoryId);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Factory not found");
    }
}