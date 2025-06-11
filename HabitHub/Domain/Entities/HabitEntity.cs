using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

[Table("Habits")]
public class HabitEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public UserEntity User { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public HabitType Type { get; set; }

    [Column(TypeName = "varchar(50)")]
    public PhysicalActivityType? PhysicalActivityType { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Goal { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [InverseProperty("Habit")]
    public ICollection<PostEntity> Posts { get; set; }

    [InverseProperty("Habit")]
    public ICollection<HabitProgressEntity> HabitProgress { get; set; }
}