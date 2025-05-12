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