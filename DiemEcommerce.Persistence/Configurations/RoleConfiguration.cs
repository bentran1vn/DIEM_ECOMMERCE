using DiemEcommerce.Contract.Constant.SystemRoles;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence.Constrants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiemEcommerce.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> builder)
    {
        builder.ToTable(TableNames.Role);

        var roles = new List<Roles>()
        {
            new ()
            {
                Id = new Guid("662904d3-6b20-437f-842c-d7e1c52bdf63"),
                Name = RoleNames.Admin,
            },
            new ()
            {
                Id = new Guid("6a900888-430b-4073-a2f4-824659ff36bf"),
                Name = RoleNames.Factory,
            },
            new ()
            {
                Id = new Guid("5a900888-430b-4073-a2f4-824659ff36bf"),
                Name = RoleNames.Customer,
            }
        };

        builder.HasData(roles);
    }
}