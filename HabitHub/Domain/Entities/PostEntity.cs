using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Posts")]
public class PostEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public UserEntity User { get; set; }
    
    [Required]
    [ForeignKey("Habit")]
    public Guid HabitId { get; set; }
    public HabitEntity Habit { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }

    [Required]
    public DateTime DateTime { get; set; }

    [InverseProperty("Post")]
    public ICollection<MediaFileEntity> MediaFiles { get; set; }

    [InverseProperty("Post")]
    public ICollection<CommentEntity> Comments { get; set; }

    [InverseProperty("Post")]
    public ICollection<LikeEntity> Likes { get; set; }
}