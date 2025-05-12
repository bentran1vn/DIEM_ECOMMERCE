using System.Linq.Expressions;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Commands.Factory;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Commands.Factory;

public class CreateFactoryCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>> _factoryRepositoryMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>> _userRepositoryMock;
    private readonly Mock<IMediaService> _mediaServiceMock;
    private readonly CreateFactoryCommandHandler _handler;

    public CreateFactoryCommandHandlerTests()
    {
        _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>>();
        _userRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Users, Guid>>();
        _mediaServiceMock = new Mock<IMediaService>();
        
        _handler = new CreateFactoryCommandHandler(
            _factoryRepositoryMock.Object,
            _mediaServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Users { Id = userId, Email = "test@example.com" };
        
        var formFile = new Mock<IFormFile>();
        formFile.Setup(f => f.Length).Returns(100);
        formFile.Setup(f => f.FileName).Returns("logo.jpg");
        
        var body = new Contract.Services.Factory.Commands.CreateFactoryBody
        {
            Name = "Test Factory",
            Description = "Test Description",
            Address = "Test Address",
            PhoneNumber = "123456789",
            Email = "factory@example.com",
            Website = "http://factory.com",
            Logo = formFile.Object,
            TaxCode = "TAX123",
            BankAccount = "BANK123",
            BankName = "Test Bank"
        };
        
        var command = new Contract.Services.Factory.Commands.CreateFactoryCommand(body, userId);

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _factoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Factories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        _mediaServiceMock
            .Setup(x => x.UploadImageAsync(formFile.Object))
            .ReturnsAsync("http://example.com/logo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _factoryRepositoryMock.Verify(x => x.Add(It.IsAny<Factories>()), Times.Once);
        user.FactoriesId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithExistingFactoryName_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new Users { Id = userId, Email = "test@example.com" };
        var existingFactory = new Factories { Name = "Test Factory" };
        
        var formFile = new Mock<IFormFile>();
        var body = new Contract.Services.Factory.Commands.CreateFactoryBody
        {
            Name = "Test Factory",
            Description = "Test Description",
            Address = "Test Address",
            PhoneNumber = "123456789",
            Email = "factory@example.com",
            Website = "http://factory.com",
            Logo = formFile.Object,
            TaxCode = "TAX123",
            BankAccount = "BANK123",
            BankName = "Test Bank"
        };
        
        var command = new Contract.Services.Factory.Commands.CreateFactoryCommand(body, userId);

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))
            .ReturnsAsync(user);

        _factoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Factories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(existingFactory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("500");
        result.Error.Message.Should().Be("Factory with this name already exists");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var formFile = new Mock<IFormFile>();
        var body = new Contract.Services.Factory.Commands.CreateFactoryBody
        {
            Name = "Test Factory",
            Description = "Test Description",
            Address = "Test Address",
            PhoneNumber = "123456789",
            Email = "factory@example.com",
            Website = "http://factory.com",
            Logo = formFile.Object,
            TaxCode = "TAX123",
            BankAccount = "BANK123",
            BankName = "Test Bank"
        };
        
        var command = new Contract.Services.Factory.Commands.CreateFactoryCommand(body, userId);

        _userRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Users, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Users, object>>[]>()))!
            .ReturnsAsync((Users)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("500");
        result.Error.Message.Should().Be("User not found");
    }
}

public class UpdateFactoryCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>> _factoryRepositoryMock;
    private readonly Mock<IMediaService> _mediaServiceMock;
    private readonly UpdateFactoryCommandHandler _handler;

    public UpdateFactoryCommandHandlerTests()
    {
        _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>>();
        _mediaServiceMock = new Mock<IMediaService>();
        
        _handler = new UpdateFactoryCommandHandler(
            _factoryRepositoryMock.Object,
            _mediaServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingFactory = new Factories 
        { 
            Id = factoryId, 
            Name = "Old Name",
            IsDeleted = false 
        };
        
        var body = new Contract.Services.Factory.Commands.UpdateFactoryBody
        {
            Id = factoryId,
            Name = "New Factory Name",
            Description = "New Description",
            Address = "New Address",
            PhoneNumber = "987654321",
            Email = "newfactory@example.com",
            Website = "http://newfactory.com",
            Logo = null,
            TaxCode = "NEWTAX",
            BankAccount = "NEWBANK",
            BankName = "New Bank"
        };
        
        var command = new Contract.Services.Factory.Commands.UpdateFactoryCommand(body, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(existingFactory);

        _factoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Factories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingFactory.Name.Should().Be("New Factory Name");
        existingFactory.Email.Should().Be("newfactory@example.com");
    }

    [Fact]
    public async Task Handle_WithNewLogo_ShouldUploadAndUpdate()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingFactory = new Factories 
        { 
            Id = factoryId, 
            Name = "Old Name",
            Logo = "old-logo.jpg",
            IsDeleted = false 
        };
        
        var formFile = new Mock<IFormFile>();
        formFile.Setup(f => f.Length).Returns(100);
        formFile.Setup(f => f.FileName).Returns("new-logo.jpg");
        
        var body = new Contract.Services.Factory.Commands.UpdateFactoryBody
        {
            Id = factoryId,
            Name = "New Factory Name",
            Description = "New Description",
            Address = "New Address",
            PhoneNumber = "987654321",
            Email = "newfactory@example.com",
            Website = "http://newfactory.com",
            Logo = formFile.Object,
            TaxCode = "NEWTAX",
            BankAccount = "NEWBANK",
            BankName = "New Bank"
        };
        
        var command = new Contract.Services.Factory.Commands.UpdateFactoryCommand(body, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(existingFactory);

        _factoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Factories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        _mediaServiceMock
            .Setup(x => x.UploadImageAsync(formFile.Object))
            .ReturnsAsync("http://example.com/new-logo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingFactory.Logo.Should().Be("http://example.com/new-logo.jpg");
        _mediaServiceMock.Verify(x => x.UploadImageAsync(formFile.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentFactory_ShouldFail()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var body = new Contract.Services.Factory.Commands.UpdateFactoryBody
        {
            Id = factoryId,
            Name = "New Factory Name"
        };
        
        var command = new Contract.Services.Factory.Commands.UpdateFactoryCommand(body, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("500");
        result.Error.Message.Should().Be("Factory not found");
    }
}

public class DeleteFactoryCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>> _factoryRepositoryMock;
    private readonly DeleteFactoryCommandHandler _handler;

    public DeleteFactoryCommandHandlerTests()
    {
        _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>>();
        _handler = new DeleteFactoryCommandHandler(_factoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingFactory = new Factories 
        { 
            Id = factoryId, 
            Name = "Test Factory",
            IsDeleted = false 
        };
        var command = new Contract.Services.Factory.Commands.DeleteFactoryCommand(factoryId, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(existingFactory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // result.Message.Should().Be("Factory deleted successfully");
    }

    [Fact]
    public async Task Handle_WithNonExistentFactory_ShouldFail()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new Contract.Services.Factory.Commands.DeleteFactoryCommand(factoryId, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))!
            .ReturnsAsync((Factories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Factory not found");
    }

    [Fact]
    public async Task Handle_WithDeletedFactory_ShouldFail()
    {
        // Arrange
        var factoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deletedFactory = new Factories 
        { 
            Id = factoryId, 
            Name = "Deleted Factory",
            IsDeleted = true
        };
        var command = new Contract.Services.Factory.Commands.DeleteFactoryCommand(factoryId, userId);

        _factoryRepositoryMock
            .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
            .ReturnsAsync(deletedFactory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Factory not found");
    }
}