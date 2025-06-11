using Domain.Enums;

namespace Application.Dto_s.Habit;

public class HabitInfoDto
{
    public Guid Id { get; set; }
    public HabitType Type { get; set; }
    public PhysicalActivityType? PhysicalActivityType { get; set; }
    public string Goal { get; set; }
    public bool IsActive { get; set; }
}