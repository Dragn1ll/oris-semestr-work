using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Comments")]
public class CommentEntity
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

    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }
    
    [Required]
    public DateTime DateTime { get; set; }
}