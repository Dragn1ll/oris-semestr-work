using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFileEntity>
{
    public void Configure(EntityTypeBuilder<MediaFileEntity> builder)
    {
        builder.Property(m => m.Type)
            .HasConversion<string>();
        
        builder.HasOne(m => m.Post)
            .WithMany(h => h.MediaFiles)
            .OnDelete(DeleteBehavior.Cascade);
    }
}