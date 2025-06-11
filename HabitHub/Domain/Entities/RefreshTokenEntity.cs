using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class RefreshTokenEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public UserEntity User { get; set; }
    [Required]
    public string Token { get; set; }
    [Required]
    public DateTime Expires { get; set; }
    [Required]
    public bool IsRevoked { get; set; }
}