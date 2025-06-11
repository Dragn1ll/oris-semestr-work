using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.DataAccess.Configurations;

namespace Persistence.DataAccess;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<HabitEntity> Habits { get; set; }
    public DbSet<PostEntity> Posts { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<MediaFileEntity> MediaFiles { get; set; }
    public DbSet<CommentEntity> Comments { get; set; }
    public DbSet<LikeEntity> Likes { get; set; }
    public DbSet<HabitProgressEntity> HabitProgress { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}