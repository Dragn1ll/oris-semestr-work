using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Likes")]
public class LikeEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public UserEntity User { get; set; }
    
    [Required]
    [ForeignKey("Post")]
    public Guid PostId { get; set; }
    public PostEntity Post { get; set; }
}