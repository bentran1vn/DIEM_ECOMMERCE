using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Commands.Category;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Commands.Category;

public class CreateCategoryCommandHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>> _categoryRepositoryMock;
        private readonly CreateCategoryCommandHandler _handler;

        public CreateCategoryCommandHandlerTests()
        {
            _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>>();
            _handler = new CreateCategoryCommandHandler(_categoryRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldSucceed()
        {
            // Arrange
            var command = new Contract.Services.Category.Commands.CreateCategoryCommand(
                "Test Category",
                "Test Description",
                null);

            _categoryRepositoryMock
                .Setup(x => x.FindSingleAsync(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))!
                .ReturnsAsync((Categories)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _categoryRepositoryMock.Verify(x => x.Add(It.IsAny<Categories>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithExistingCategoryName_ShouldFail()
        {
            // Arrange
            var command = new Contract.Services.Category.Commands.CreateCategoryCommand(
                "Existing Category",
                "Test Description",
                null);

            _categoryRepositoryMock
                .Setup(x => x.FindSingleAsync(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))
                .ReturnsAsync(new Categories { Name = "Existing Category" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("400");
            result.Error.Message.Should().Be("Exist category name");
        }

        [Fact]
        public async Task Handle_WithInvalidParentId_ShouldFail()
        {
            // Arrange
            var command = new Contract.Services.Category.Commands.CreateCategoryCommand(
                "Test Category",
                "Test Description",
                Guid.NewGuid());

            _categoryRepositoryMock
                .Setup(x => x.FindSingleAsync(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))!
                .ReturnsAsync((Categories)null);

            _categoryRepositoryMock
                .Setup(x => x.FindByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))!
                .ReturnsAsync((Categories)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("400");
            result.Error.Message.Should().Be("Parent category not found");
        }
    }