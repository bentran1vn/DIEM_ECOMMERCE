using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence.Constrants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiemEcommerce.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        builder.ToTable(TableNames.User);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(100).IsRequired(true);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired(true);
        builder.Property(x => x.Password).HasMaxLength(200).IsRequired(true);
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired(true);
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired(true);
        builder.Property(x => x.RolesId).IsRequired(true);
        builder.Property(x => x.PhoneNumber).HasMaxLength(100).IsRequired(true);
    }
}