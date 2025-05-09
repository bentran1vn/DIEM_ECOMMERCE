using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Queries.Category;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Category;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Category;

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
        var categories = new List<Categories>
        {
            new() { Id = Guid.NewGuid(), Name = "Category 1", Description = "Description 1", IsParent = true },
            new() { Id = Guid.NewGuid(), Name = "Category 2", Description = "Description 2", IsParent = false, ParentId = Guid.NewGuid() }
        };

        // Convert to Responses.CategoryResponse objects to match what we're querying
        var categoryResponses = categories
            .Select(c => new Responses.CategoryResponse(c.Id, c.Name, c.Description, c.ParentId, c.IsParent))
            .ToList();

        // Create mock async queryable that supports EF Core async operations
        var asyncQueryable = new DiemEcommerce.Application.Tests.Helpers.TestAsyncEnumerable<Responses.CategoryResponse>(categoryResponses).AsQueryable();

        _categoryRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<Expression<Func<Categories, bool>>>(), It.IsAny<Expression<Func<Categories, object>>[]>()))
            .Returns(categories.AsQueryable());

        // Critical change: We need to setup the repository to return async queryable correctly
        // Create a DbSet mock that will handle the async operations
        var mockDbSet = new Mock<DbSet<Responses.CategoryResponse>>();
        mockDbSet.As<IAsyncEnumerable<Responses.CategoryResponse>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new DiemEcommerce.Application.Tests.Helpers.TestAsyncEnumerator<Responses.CategoryResponse>(categoryResponses.GetEnumerator()));
        
        mockDbSet.As<IQueryable<Responses.CategoryResponse>>()
            .Setup(m => m.Provider)
            .Returns(new DiemEcommerce.Application.Tests.Helpers.TestAsyncQueryProvider<Responses.CategoryResponse>(asyncQueryable.Provider));
        
        mockDbSet.As<IQueryable<Responses.CategoryResponse>>()
            .Setup(m => m.Expression)
            .Returns(asyncQueryable.Expression);
        
        mockDbSet.As<IQueryable<Responses.CategoryResponse>>()
            .Setup(m => m.ElementType)
            .Returns(asyncQueryable.ElementType);
        
        mockDbSet.As<IQueryable<Responses.CategoryResponse>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => asyncQueryable.GetEnumerator());

        // Act
        var result = await _handler.Handle(new Contract.Services.Category.Queries.GetAllCategoriesQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        
        result.Value[0].Id.Should().Be(categories[0].Id);
        result.Value[0].Name.Should().Be(categories[0].Name);
        result.Value[0].IsParent.Should().BeTrue();
        
        result.Value[1].Id.Should().Be(categories[1].Id);
        result.Value[1].Name.Should().Be(categories[1].Name);
        result.Value[1].IsParent.Should().BeFalse();
    }
}