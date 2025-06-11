namespace Application.Dto_s.Habit;

public class HabitProgressAddDto
{
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public float PercentageCompletion { get; set; }
}