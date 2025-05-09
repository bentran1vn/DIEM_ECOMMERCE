using DiemEcommerce.Domain.Entities;
using FluentAssertions;

namespace DiemEcommerce.Domain.Tests.Entities;

public class UsersTests
{
    [Fact]
    public void User_FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange
        var user = new Users()
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        string fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }
}

public class EntityTests
{
    [Fact]
    public void Entity_IsDeleted_ShouldDefaultToFalse()
    {
        // Arrange
        var entity = new Customers
        {
            Id = Guid.NewGuid()
        };

        // Assert
        entity.IsDeleted.Should().BeFalse();
    }
}