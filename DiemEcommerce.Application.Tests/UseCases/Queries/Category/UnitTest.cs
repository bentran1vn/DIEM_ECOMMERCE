using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Commands.Category;
using DiemEcommerce.Application.UseCases.Queries.Category;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Category;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Category
{
    // Category Tests
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
                    It.IsAny<Expression<Func<Categories, object>>[]>()))
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

    public class GetAllCategoriesQueryHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Categories, Guid>> _categoryRepositoryMock;
        private readonly GetAllCategoriesQueryHandler _handler;

        public GetAllCategoriesQueryHandlerTests()
        {
            _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Categories, Guid>>();
            _handler = new GetAllCategoriesQueryHandler(_categoryRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnAllCategories()
        {
            // Arrange
            var categoryId1 = Guid.NewGuid();
            var categoryId2 = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            
            var categories = new List<Categories>
            {
                new() 
                { 
                    Id = categoryId1, 
                    Name = "Category 1", 
                    Description = "Description 1", 
                    IsParent = true,
                    IsDeleted = false,
                    ParentId = null
                },
                new() 
                { 
                    Id = categoryId2, 
                    Name = "Category 2", 
                    Description = "Description 2", 
                    IsParent = false, 
                    ParentId = parentId,
                    IsDeleted = false
                }
            };

            // Use MockQueryable to create an async queryable
            var mockQueryable = categories.AsQueryable().BuildMock();

            _categoryRepositoryMock
                .Setup(x => x.FindAll(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))
                .Returns(mockQueryable); // Remove .Object here

            // Act
            var result = await _handler.Handle(new Contract.Services.Category.Queries.GetAllCategoriesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            
            var firstCategory = result.Value.FirstOrDefault(c => c.Id == categoryId1);
            firstCategory.Should().NotBeNull();
            firstCategory!.Name.Should().Be("Category 1");
            firstCategory.IsParent.Should().BeTrue();
            firstCategory.ParentId.Should().BeNull();
            
            var secondCategory = result.Value.FirstOrDefault(c => c.Id == categoryId2);
            secondCategory.Should().NotBeNull();
            secondCategory!.Name.Should().Be("Category 2");
            secondCategory.IsParent.Should().BeFalse();
            secondCategory.ParentId.Should().Be(parentId);
        }

        [Fact]
        public async Task Handle_ShouldFilterOutDeletedCategories()
        {
            // Arrange - Only return non-deleted categories
            var activeCategories = new List<Categories>
            {
                new() 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Active Category", 
                    Description = "Active Description", 
                    IsParent = true,
                    IsDeleted = false
                }
            };

            var mockQueryable = activeCategories.AsQueryable().BuildMock();

            _categoryRepositoryMock
                .Setup(x => x.FindAll(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))
                .Returns(mockQueryable); // Remove .Object here

            // Act
            var result = await _handler.Handle(new Contract.Services.Category.Queries.GetAllCategoriesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Name.Should().Be("Active Category");
        }

        [Fact]
        public async Task Handle_WithNoCategories_ShouldReturnEmptyList()
        {
            // Arrange
            var categories = new List<Categories>();
            var mockQueryable = categories.AsQueryable().BuildMock();

            _categoryRepositoryMock
                .Setup(x => x.FindAll(
                    It.IsAny<Expression<Func<Categories, bool>>>(),
                    It.IsAny<Expression<Func<Categories, object>>[]>()))
                .Returns(mockQueryable); // Remove .Object here

            // Act
            var result = await _handler.Handle(new Contract.Services.Category.Queries.GetAllCategoriesQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
    }
}