using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class HabitProgressEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [ForeignKey("Habit")]
    public Guid HabitId { get; set; }
    public HabitEntity Habit { get; set; }
    
    [Required] 
    [Column(TypeName = "date")]
    public DateOnly Date { get; set; }
    
    [Required]
    public float PercentageCompletion { get; set; }
}