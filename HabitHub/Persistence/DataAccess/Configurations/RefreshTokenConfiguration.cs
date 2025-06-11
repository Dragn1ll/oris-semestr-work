using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.User)
            .WithOne(x => x.RefreshToken)
            .HasForeignKey<RefreshTokenEntity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}