using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.DataAccess.Configurations;

public class HabitProgressConfiguration : IEntityTypeConfiguration<HabitProgressEntity>
{
    public void Configure(EntityTypeBuilder<HabitProgressEntity> builder)
    {
        builder.HasOne(hp => hp.Habit)
            .WithMany(h => h.HabitProgress)
            .HasForeignKey(hp => hp.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}