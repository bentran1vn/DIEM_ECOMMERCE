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
    
public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>> _categoryRepositoryMock;
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>>();
        _handler = new UpdateCategoryCommandHandler(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Old Name", 
            Description = "Old Description",
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "New Category Name",
            "New Description",
            null);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // No existing category with the same name (null means no conflict)
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
        existingCategory.Name.Should().Be("New Category Name");
        existingCategory.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "New Name",
            "New Description",
            null);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))!
            .ReturnsAsync((Categories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("400");
        result.Error.Message.Should().Be("Category not found");
    }

    [Fact]
    public async Task Handle_WithExistingCategoryName_ShouldFail()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Old Name", 
            Description = "Old Description",
            IsDeleted = false 
        };
        var conflictCategory = new Categories 
        { 
            Id = Guid.NewGuid(), 
            Name = "Existing Name",
            IsParent = false, // The handler checks for !x.IsParent
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "Existing Name", // Using a name that already exists
            "New Description",
            null);

        // Set up the main category lookup to find the existing category
        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // Set up the name conflict check - return the conflicting category
        _categoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Categories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(conflictCategory);

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
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Old Name", 
            Description = "Old Description",
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "New Name",
            "New Description",
            parentId);

        // Set up to find the existing category
        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // No name conflict
        _categoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Categories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Categories, object>>[]>()))!
            .ReturnsAsync((Categories)null);

        // Parent category doesn't exist
        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(parentId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))!
            .ReturnsAsync((Categories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("400");
        result.Error.Message.Should().Be("Parent category not found");
    }

    [Fact]
    public async Task Handle_WithValidParentId_ShouldSucceed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Old Name", 
            Description = "Old Description",
            IsDeleted = false,
            ParentId = null // Initially no parent
        };
        var parentCategory = new Categories 
        { 
            Id = parentId, 
            Name = "Parent Category",
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "New Name",
            "New Description",
            parentId);

        // Set up to find the existing category
        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // No name conflict
        _categoryRepositoryMock
            .Setup(x => x.FindSingleAsync(
                It.IsAny<Expression<Func<Categories, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Categories, object>>[]>()))!
            .ReturnsAsync((Categories)null);

        // Parent category exists
        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(parentId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(parentCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCategory.Name.Should().Be("New Name");
        existingCategory.Description.Should().Be("New Description");
        existingCategory.ParentId.Should().Be(parentId);
    }

    [Fact]
    public async Task Handle_WithSameNameButIsParent_ShouldSucceed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Old Name", 
            Description = "Old Description",
            IsDeleted = false 
        };
        var parentCategoryWithSameName = new Categories 
        { 
            Id = Guid.NewGuid(), 
            Name = "New Category Name",
            IsParent = true, // This is a parent category, so it shouldn't conflict
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.UpdateCategoryCommand(
            categoryId,
            "New Category Name", // Same name as parent category
            "New Description",
            null);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // The handler only checks for conflicts with non-parent categories
        // So this should return null since the matching category is a parent
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
        existingCategory.Name.Should().Be("New Category Name");
        existingCategory.Description.Should().Be("New Description");
    }
}

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>> _categoryRepositoryMock;
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepositoryBase<ApplicationDbContext, Categories, Guid>>();
        _handler = new DeleteCategoryCommandHandler(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Test Category", 
            Description = "Test Description",
            IsDeleted = false 
        };
        var command = new Contract.Services.Category.Commands.DeleteCategoryCommand(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _categoryRepositoryMock.Verify(x => x.Remove(existingCategory), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new Contract.Services.Category.Commands.DeleteCategoryCommand(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))!
            .ReturnsAsync((Categories)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Category not found");
        _categoryRepositoryMock.Verify(x => x.Remove(It.IsAny<Categories>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeletedCategory_ShouldFail()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var deletedCategory = new Categories 
        { 
            Id = categoryId, 
            Name = "Deleted Category", 
            Description = "Deleted Description",
            IsDeleted = true
        };
        var command = new Contract.Services.Category.Commands.DeleteCategoryCommand(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FindByIdAsync(categoryId, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .ReturnsAsync(deletedCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Category not found");
        _categoryRepositoryMock.Verify(x => x.Remove(It.IsAny<Categories>()), Times.Never);
    }
}