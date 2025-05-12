using System.Linq.Expressions;
using DiemEcommerce.Application.UseCases.Queries.Match;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using FluentAssertions;
using MockQueryable;
using Moq;

namespace DiemEcommerce.Application.Tests.UseCases.Queries.Match
{
    public class GetAllMatchesQueryHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid>> _matchRepositoryMock;
        private readonly GetAllMatchesQueryHandler _handler;

        public GetAllMatchesQueryHandlerTests()
        {
            _matchRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid>>();
            _handler = new GetAllMatchesQueryHandler(_matchRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidQuery_ShouldReturnMatchesPage()
        {
            // Arrange
            var matchId1 = Guid.NewGuid();
            var matchId2 = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();

            var matches = new List<Matches>
            {
                new() 
                { 
                    Id = matchId1,
                    Name = "Match 1",
                    Description = "Description 1",
                    Price = 100m,
                    Quantity = 10,
                    IsDeleted = false,
                    CategoriesId = categoryId,
                    FactoriesId = factoryId,
                    Categories = new Categories { Id = categoryId, Name = "Category 1" },
                    Factories = new Factories { Id = factoryId, Name = "Factory 1", Address = "Address 1", PhoneNumber = "123456789" },
                    CoverImages = new List<MatchMedias>
                    {
                        new() { Id = Guid.NewGuid(), Url = "http://example.com/image1.jpg", IsDeleted = false }
                    }
                },
                new() 
                { 
                    Id = matchId2,
                    Name = "Match 2",
                    Description = "Description 2",
                    Price = 200m,
                    Quantity = 20,
                    IsDeleted = false,
                    CategoriesId = categoryId,
                    FactoriesId = factoryId,
                    Categories = new Categories { Id = categoryId, Name = "Category 1" },
                    Factories = new Factories { Id = factoryId, Name = "Factory 1", Address = "Address 1", PhoneNumber = "123456789" },
                    CoverImages = new List<MatchMedias>
                    {
                        new() { Id = Guid.NewGuid(), Url = "http://example.com/image2.jpg", IsDeleted = false }
                    }
                }
            };

            var query = matches.AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(query);

            var request = new Contract.Services.Match.Queries.GetAllMatchQuery(null, null, 1, 10);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items[0].Name.Should().Be("Match 1");
            result.Value.Items[1].Name.Should().Be("Match 2");
        }

        [Fact]
        public async Task Handle_WithCategoryFilter_ShouldReturnFilteredMatches()
        {
            // Arrange
            var categoryId1 = Guid.NewGuid();
            var categoryId2 = Guid.NewGuid();
            var factoryId = Guid.NewGuid();

            var matches = new List<Matches>
            {
                new() 
                { 
                    Id = Guid.NewGuid(),
                    Name = "Match 1",
                    Description = "Description 1",
                    Price = 100m,
                    Quantity = 10,
                    IsDeleted = false,
                    CategoriesId = categoryId1,
                    FactoriesId = factoryId,
                    Categories = new Categories { Id = categoryId1, Name = "Category 1" },
                    Factories = new Factories { Id = factoryId, Name = "Factory 1", Address = "Address 1", PhoneNumber = "123456789" },
                    CoverImages = new List<MatchMedias>()
                },
                new() 
                { 
                    Id = Guid.NewGuid(),
                    Name = "Match 2",
                    Description = "Description 2",
                    Price = 200m,
                    Quantity = 20,
                    IsDeleted = false,
                    CategoriesId = categoryId2,
                    FactoriesId = factoryId,
                    Categories = new Categories { Id = categoryId2, Name = "Category 2" },
                    Factories = new Factories { Id = factoryId, Name = "Factory 1", Address = "Address 1", PhoneNumber = "123456789" },
                    CoverImages = new List<MatchMedias>()
                }
            };

            // Filter only matches with categoryId1
            var filteredMatches = matches.Where(m => m.CategoriesId == categoryId1).AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(filteredMatches);

            var request = new Contract.Services.Match.Queries.GetAllMatchQuery(new List<Guid> { categoryId1 }, null, 1, 10);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].CategoryId.Should().Be(categoryId1);
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnFilteredMatches()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();

            var matches = new List<Matches>
            {
                new() 
                { 
                    Id = Guid.NewGuid(),
                    Name = "Electronics Match",
                    Description = "Electronic device description",
                    Price = 100m,
                    Quantity = 10,
                    IsDeleted = false,
                    CategoriesId = categoryId,
                    FactoriesId = factoryId,
                    Categories = new Categories { Id = categoryId, Name = "Category 1" },
                    Factories = new Factories { Id = factoryId, Name = "Factory 1", Address = "Address 1", PhoneNumber = "123456789" },
                    CoverImages = new List<MatchMedias>()
                }
            };

            // Filter matches containing "electronics"
            var filteredMatches = matches.Where(m => 
                m.Name.ToLower().Contains("electronics") || 
                m.Description.ToLower().Contains("electronics")).AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(filteredMatches);

            var request = new Contract.Services.Match.Queries.GetAllMatchQuery(null, "electronics", 1, 10);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Electronics Match");
        }
    }

    public class GetMatchByIdQueryHandlerTests
    {
        private readonly Mock<IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid>> _matchRepositoryMock;
        private readonly GetMatchByIdQueryHandler _handler;

        public GetMatchByIdQueryHandlerTests()
        {
            _matchRepositoryMock = new Mock<IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid>>();
            _handler = new GetMatchByIdQueryHandler(_matchRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidId_ShouldReturnMatchDetails()
        {
            // Arrange
            var matchId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var factoryId = Guid.NewGuid();

            var match = new Matches
            {
                Id = matchId,
                Name = "Test Match",
                Description = "Test Description",
                Price = 100m,
                Quantity = 10,
                IsDeleted = false,
                CategoriesId = categoryId,
                FactoriesId = factoryId,
                Categories = new Categories { Id = categoryId, Name = "Category 1" },
                Factories = new Factories 
                { 
                    Id = factoryId, 
                    Name = "Factory 1", 
                    Address = "Address 1", 
                    PhoneNumber = "123456789",
                    Email = "factory@example.com",
                    Website = "http://factory.com",
                    Description = "Factory Description",
                    TaxCode = "TAX123",
                    BankAccount = "BANK123",
                    BankName = "Bank Name",
                    Logo = "http://example.com/logo.jpg"
                },
                CoverImages = new List<MatchMedias>
                {
                    new() { Id = Guid.NewGuid(), Url = "http://example.com/image1.jpg", IsDeleted = false }
                }
            };

            var matches = new List<Matches> { match }.AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(matches);

            var request = new Contract.Services.Match.Queries.GetMatchByIdQuery(matchId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var responseMatch = result.Value;
            responseMatch.Id.Should().Be(matchId);
            responseMatch.Name.Should().Be("Test Match");
            responseMatch.Description.Should().Be("Test Description");
            responseMatch.Price.Should().Be(100m);
            responseMatch.Quantity.Should().Be(10);
            responseMatch.CategoryId.Should().Be(categoryId);
            responseMatch.CategoryName.Should().Be("Category 1");
            responseMatch.FactoryId.Should().Be(factoryId);
            responseMatch.FactoryName.Should().Be("Factory 1");
            responseMatch.FactoryEmail.Should().Be("factory@example.com");
            responseMatch.CoverImages.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var matchId = Guid.NewGuid();

            var matches = new List<Matches>().AsQueryable().BuildMock();
            _matchRepositoryMock
                .Setup(x => x.FindAll(It.IsAny<Expression<Func<Matches, bool>>>(), It.IsAny<Expression<Func<Matches, object>>[]>()))
                .Returns(matches);

            var request = new Contract.Services.Match.Queries.GetMatchByIdQuery(matchId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("404");
            result.Error.Message.Should().Be("Match not found");
        }
    }
}