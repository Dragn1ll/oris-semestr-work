using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<PostEntity>
{
    public void Configure(EntityTypeBuilder<PostEntity> builder)
    {
        builder.HasIndex(p => new { p.UserId, p.HabitId });

        builder.HasOne(p => p.Habit)
            .WithMany(h => h.Posts)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.User)
            .WithMany(h => h.Posts)
            .OnDelete(DeleteBehavior.Cascade);
    }
}