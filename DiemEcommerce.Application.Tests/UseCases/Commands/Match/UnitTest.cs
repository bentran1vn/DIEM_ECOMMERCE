using System.Linq.Expressions;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.UseCases.Commands.Match;
using DiemEcommerce.Application.UseCases.Queries.Match;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Match;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace DiemEcommerce.Application.Tests.UseCases.Commands.Match
{
    public class CreateMatchCommandHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>> _matchRepositoryMock;
        private readonly Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>> _categoryRepositoryMock;
        private readonly Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>> _factoryRepositoryMock;
        private readonly Mock<IMediaService> _mediaServiceMock;
        private readonly CreateMatchCommandHandler _handler;

        public CreateMatchCommandHandlerTests()
        {
            _matchRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>>();
            _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>>();
            _factoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Factories, Guid>>();
            _mediaServiceMock = new Mock<IMediaService>();
            
            _handler = new CreateMatchCommandHandler(
                _matchRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _factoryRepositoryMock.Object,
                _mediaServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldSucceed()
        {
            // Arrange
            var factoryId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            
            var factory = new Factories { Id = factoryId, IsDeleted = false };
            var category = new Categories { Id = categoryId, IsDeleted = false };
            
            var body = new Contract.Services.Match.Commands.CreateMatchBody
            {
                Name = "Test Match",
                Description = "Test Description",
                Price = 100m,
                Quantity = 10,
                CategoryId = categoryId,
                CoverImages = MockFormFileCollection()
            };
            
            var command = new Contract.Services.Match.Commands.CreateMatchCommand(factoryId, body);

            _factoryRepositoryMock
                .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
                .ReturnsAsync(factory);

            _categoryRepositoryMock
                .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
                .ReturnsAsync(category);

            var matches = new List<Matches>().AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(matches);

            _mediaServiceMock
                .Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("http://example.com/image.jpg");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _matchRepositoryMock.Verify(x => x.Add(It.IsAny<Matches>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentFactory_ShouldFail()
        {
            // Arrange
            var factoryId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            
            var body = new Contract.Services.Match.Commands.CreateMatchBody
            {
                Name = "Test Match",
                Description = "Test Description",
                Price = 100m,
                Quantity = 10,
                CategoryId = categoryId,
                CoverImages = MockFormFileCollection()
            };
            
            var command = new Contract.Services.Match.Commands.CreateMatchCommand(factoryId, body);

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
        public async Task Handle_WithExistingMatchName_ShouldFail()
        {
            // Arrange
            var factoryId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            
            var factory = new Factories { Id = factoryId, IsDeleted = false };
            var category = new Categories { Id = categoryId, IsDeleted = false };
            
            var body = new Contract.Services.Match.Commands.CreateMatchBody
            {
                Name = "Existing Match",
                Description = "Test Description",
                Price = 100m,
                Quantity = 10,
                CategoryId = categoryId,
                CoverImages = MockFormFileCollection()
            };
            
            var command = new Contract.Services.Match.Commands.CreateMatchCommand(factoryId, body);

            _factoryRepositoryMock
                .Setup(x => x.FindByIdAsync(factoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Factories, object>>[]>()))
                .ReturnsAsync(factory);

            _categoryRepositoryMock
                .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
                .ReturnsAsync(category);

            var existingMatch = new Matches { Name = "existing match", Description = "test description", Price = 100m, Quantity = 10 ,FactoriesId = factoryId, IsDeleted = false };
            var matches = new List<Matches> { existingMatch }.AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(matches);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("409");
            result.Error.Message.Should().Be("A match with this name already exists for this factory");
        }

        private IFormFileCollection MockFormFileCollection()
        {
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.Length).Returns(100);
            formFile.Setup(f => f.FileName).Returns("test.jpg");
            
            var formFileCollection = new Mock<IFormFileCollection>();
            formFileCollection.Setup(f => f.GetEnumerator()).Returns(new List<IFormFile> { formFile.Object }.GetEnumerator());
            formFileCollection.Setup(f => f.Count).Returns(1);
            formFileCollection.Setup(f => f[It.IsAny<int>()]).Returns(formFile.Object);
            
            return formFileCollection.Object;
        }
    }
    
    public class UpdateMatchCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>> _matchRepositoryMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, MatchMedias, Guid>> _matchMediaRepositoryMock;
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>> _categoryRepositoryMock;
    private readonly Mock<IMediaService> _mediaServiceMock;
    private readonly UpdateMatchCommandHandler _handler;

    public UpdateMatchCommandHandlerTests()
    {
        _matchRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>>();
        _matchMediaRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, MatchMedias, Guid>>();
        _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>>();
        _mediaServiceMock = new Mock<IMediaService>();
        
        _handler = new UpdateMatchCommandHandler(
            _matchRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _mediaServiceMock.Object,
            _matchMediaRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var factoryId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        
        var existingMatch = new Matches 
        { 
            Id = matchId,
            Name = "Old Match",
            Description = "Old Description",
            Price = 100m,
            Quantity = 10,
            FactoriesId = factoryId,
            CategoriesId = Guid.NewGuid(),
            IsDeleted = false 
        };
        
        var category = new Categories { Id = categoryId, IsDeleted = false };
        
        var body = new Contract.Services.Match.Commands.UpdateMatchBody
        {
            Id = matchId,
            Name = "Updated Match",
            Description = "Updated Description",
            Price = "200",
            Quantity = "20",
            CategoryId = categoryId,
            DeleteImages = null,
            NewImages = null
        };
        
        var command = new Contract.Services.Match.Commands.UpdateMatchCommand(factoryId, body);

        _matchRepositoryMock
            .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .ReturnsAsync(existingMatch);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(category);

        var matches = new List<Matches>().AsQueryable().BuildMock();
        _matchRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .Returns(matches);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingMatch.Name.Should().Be("Updated Match");
        existingMatch.Description.Should().Be("Updated Description");
        existingMatch.Price.Should().Be(200m);
        existingMatch.Quantity.Should().Be(20);
        existingMatch.CategoriesId.Should().Be(categoryId);
    }

    [Fact]
    public async Task Handle_WithUnauthorizedFactory_ShouldFail()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var factoryId = Guid.NewGuid();
        var wrongFactoryId = Guid.NewGuid();
        
        var existingMatch = new Matches 
        { 
            Id = matchId,
            Name = "",
            Description = "",
            Price = 0,
            Quantity = 0,
            FactoriesId = factoryId, // Different from command factory
            IsDeleted = false 
        };
        
        var body = new Contract.Services.Match.Commands.UpdateMatchBody
        {
            Id = matchId,
            Name = "Updated Match"
        };
        
        var command = new Contract.Services.Match.Commands.UpdateMatchCommand(wrongFactoryId, body);

        _matchRepositoryMock
            .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .ReturnsAsync(existingMatch);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("403");
        result.Error.Message.Should().Be("You are not authorized to update this match");
    }

    [Fact]
    public async Task Handle_WithDeleteImages_ShouldRemoveImages()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var factoryId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var imageId1 = Guid.NewGuid();
        var imageId2 = Guid.NewGuid();
        
        var existingMatch = new Matches 
        { 
            Id = matchId,
            Name = "",
            Description = "",
            Price = 0,
            Quantity = 0,
            FactoriesId = factoryId,
            CategoriesId = Guid.NewGuid(),
            IsDeleted = false 
        };
        
        var category = new Categories { Id = categoryId, IsDeleted = false };
        
        var imagesToDelete = new List<MatchMedias>
        {
            new() { Id = imageId1, MatchesId = matchId },
            new() { Id = imageId2, MatchesId = matchId }
        };
        
        var body = new Contract.Services.Match.Commands.UpdateMatchBody
        {
            Id = matchId,
            Name = "Updated Match",
            Description = "Updated Description",
            Price = "200",
            Quantity = "20",
            CategoryId = categoryId,
            DeleteImages = new[] { imageId1, imageId2 },
            NewImages = null
        };
        
        var command = new Contract.Services.Match.Commands.UpdateMatchCommand(factoryId, body);

        _matchRepositoryMock
            .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .ReturnsAsync(existingMatch);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(category);

        var matches = new List<Matches>().AsQueryable().BuildMock();
        _matchRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .Returns(matches);

        var mediaQuery = imagesToDelete.AsQueryable().BuildMock();
        _matchMediaRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<MatchMedias, bool>>>(), It.IsAny<Expression<Func<MatchMedias, object>>[]>()))
            .Returns(mediaQuery);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _matchMediaRepositoryMock.Verify(x => x.RemoveMultiple(It.Is<List<MatchMedias>>(list => list.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewImages_ShouldUploadAndAddImages()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var factoryId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        
        var existingMatch = new Matches 
        { 
            Id = matchId,
            Name = "Old Match",
            Description = "Old Description",
            Price = 100m,
            Quantity = 10,
            FactoriesId = factoryId,
            CategoriesId = Guid.NewGuid(),
            IsDeleted = false 
        };
        
        var category = new Categories { Id = categoryId, IsDeleted = false };
        
        // Create actual IFormFile instances instead of mocks for better compatibility
        var formFiles = new List<IFormFile>();
        
        // Create mock files
        var formFile1 = new Mock<IFormFile>();
        formFile1.Setup(f => f.Length).Returns(100);
        formFile1.Setup(f => f.FileName).Returns("image1.jpg");
        formFiles.Add(formFile1.Object);
        
        var formFile2 = new Mock<IFormFile>();
        formFile2.Setup(f => f.Length).Returns(200);
        formFile2.Setup(f => f.FileName).Returns("image2.jpg");
        formFiles.Add(formFile2.Object);
        
        // Create a more comprehensive mock for IFormFileCollection
        var formFileCollection = new Mock<IFormFileCollection>();
        
        // Set up the indexer
        formFileCollection.Setup(f => f[0]).Returns(formFile1.Object);
        formFileCollection.Setup(f => f[1]).Returns(formFile2.Object);
        
        // Set up Count property
        formFileCollection.Setup(f => f.Count).Returns(2);
        
        // Set up GetEnumerator to return a proper enumerator
        formFileCollection.Setup(f => f.GetEnumerator())
            .Returns(() => formFiles.GetEnumerator());
        
        // Also set up the non-generic GetEnumerator
        formFileCollection.As<System.Collections.IEnumerable>()
            .Setup(f => f.GetEnumerator())
            .Returns(() => formFiles.GetEnumerator());
        
        var body = new Contract.Services.Match.Commands.UpdateMatchBody
        {
            Id = matchId,
            Name = "Updated Match",
            Description = "Updated Description",
            Price = "200",
            Quantity = "20",
            CategoryId = categoryId,
            DeleteImages = null,
            NewImages = formFileCollection.Object
        };
        
        var command = new Contract.Services.Match.Commands.UpdateMatchCommand(factoryId, body);

        _matchRepositoryMock
            .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .ReturnsAsync(existingMatch);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(category);

        var matches = new List<Matches>().AsQueryable().BuildMock();
        _matchRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
            .Returns(matches);

        _mediaServiceMock
            .Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("http://example.com/uploaded-image.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mediaServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<IFormFile>()), Times.Exactly(2));
        _matchMediaRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<MatchMedias>>(medias => medias.Count() == 2)), Times.Once);
    }
}

    public class DeleteMatchCommandHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>> _matchRepositoryMock;
        private readonly DeleteMatchCommandHandler _handler;

        public DeleteMatchCommandHandlerTests()
        {
            _matchRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Matches, Guid>>();
            _handler = new DeleteMatchCommandHandler(_matchRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldSucceed()
        {
            // Arrange
            var matchId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();
            var existingMatch = new Matches 
            { 
                Id = matchId, 
                Name = "Test Match",
                Description = "",
                Price = 0,
                Quantity = 0,
                FactoriesId = factoryId,
                IsDeleted = false 
            };
            
            var body = new Contract.Services.Match.Commands.DeleteMatchBody { Id = matchId };
            var command = new Contract.Services.Match.Commands.DeleteMatchCommand(factoryId, body);

            _matchRepositoryMock
                .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .ReturnsAsync(existingMatch);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _matchRepositoryMock.Verify(x => x.Remove(existingMatch), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentMatch_ShouldFail()
        {
            // Arrange
            var matchId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();
            
            var body = new Contract.Services.Match.Commands.DeleteMatchBody { Id = matchId };
            var command = new Contract.Services.Match.Commands.DeleteMatchCommand(factoryId, body);

            _matchRepositoryMock
                .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))!
                .ReturnsAsync((Matches)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("404");
            result.Error.Message.Should().Be("Match not found");
            _matchRepositoryMock.Verify(x => x.Remove(It.IsAny<Matches>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithDeletedMatch_ShouldFail()
        {
            // Arrange
            var matchId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();
            var deletedMatch = new Matches 
            { 
                Id = matchId, 
                Description = "",
                Price = 0,
                Quantity = 0,
                Name = "Deleted Match",
                FactoriesId = factoryId,
                IsDeleted = true
            };
            
            var body = new Contract.Services.Match.Commands.DeleteMatchBody { Id = matchId };
            var command = new Contract.Services.Match.Commands.DeleteMatchCommand(factoryId, body);

            _matchRepositoryMock
                .Setup(x => x.FindByIdAsync(matchId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .ReturnsAsync(deletedMatch);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("404");
            result.Error.Message.Should().Be("Match not found");
            _matchRepositoryMock.Verify(x => x.Remove(It.IsAny<Matches>()), Times.Never);
        }
    }
}
