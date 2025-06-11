using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Users")]
public class UserEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Surname { get; set; }
    
    [MaxLength(100)]
    public string? Patronymic { get; set; }
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateOnly BirthDay { get; set; }

    [InverseProperty("User")]
    public ICollection<HabitEntity> Habits { get; set; }

    [InverseProperty("User")]
    public ICollection<PostEntity> Posts { get; set; }

    [InverseProperty("Recipient")]
    public ICollection<MessageEntity> ReceivedMessages { get; set; }

    [InverseProperty("Sender")]
    public ICollection<MessageEntity> SentMessages { get; set; }

    [InverseProperty("User")]
    public ICollection<CommentEntity> Comments { get; set; }
    
    [InverseProperty("User")]
    public ICollection<LikeEntity> Likes { get; set; }
    
    [InverseProperty("User")]
    public RefreshTokenEntity RefreshToken { get; set; }
}