namespace Application.Dto_s.Habit;

public class HabitProgressInfoDto
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public float PercentageCompletion { get; set; }
}