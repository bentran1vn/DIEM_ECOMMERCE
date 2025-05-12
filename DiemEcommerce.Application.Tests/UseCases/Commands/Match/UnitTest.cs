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
}
