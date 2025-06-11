using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<CommentEntity>
{
    public void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(c => c.Post)
            .WithMany(u => u.Comments)
            .OnDelete(DeleteBehavior.Cascade);
    }
}