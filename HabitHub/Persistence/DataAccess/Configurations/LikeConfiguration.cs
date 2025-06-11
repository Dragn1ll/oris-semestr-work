using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class LikeConfiguration : IEntityTypeConfiguration<LikeEntity>
{
    public void Configure(EntityTypeBuilder<LikeEntity> builder)
    {
        builder.HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(l => l.Post)
            .WithMany(u => u.Likes)
            .OnDelete(DeleteBehavior.Cascade);
    }
}