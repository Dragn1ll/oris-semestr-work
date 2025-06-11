using Domain.Enums;

namespace Application.Dto_s.Habit;

public class HabitAddDto
{
    public Guid UserId { get; set; }
    public HabitType Type { get; set; }
    public PhysicalActivityType? PhysicalActivityType { get; set; }
    public string Goal { get; set; }
}