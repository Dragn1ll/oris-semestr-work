using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

[Table("MediaFiles")]
public class MediaFileEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [ForeignKey("Post")]
    public Guid PostId { get; set; }
    public PostEntity Post { get; set; }
    
    [Required]
    [Column(TypeName = "varchar(50)")]
    public MediaFileType Type { get; set; }
    
    [Required]
    [Column(TypeName = "varchar(10)")]
    public string Extension { get; set; }
}