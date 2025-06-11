using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("Messages")]
public class MessageEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("Recipient")]
    public Guid RecipientId { get; set; }
    public UserEntity Recipient { get; set; }
    
    [Required]
    [ForeignKey("Sender")]
    public Guid SenderId { get; set; }
    public UserEntity Sender { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }

    [Required]
    public DateTime DateTime { get; set; }
}