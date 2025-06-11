namespace Domain.Models;

public class HabitProgress(
    Guid id,
    Guid habitId,
    DateOnly date,
    float percentageCompletion
    )
{
    public Guid Id { get; } = id;
    public Guid HabitId { get; } = habitId;
    public DateOnly Date { get; } = date;
    public float PercentageCompletion { get; } = percentageCompletion;

    public static HabitProgress Create(Guid id, Guid habitId, DateOnly date, float percentageCompletion)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (habitId == Guid.Empty)
            throw new ArgumentException("HabitId cannot be empty");
        
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (percentageCompletion < 0)
            throw new ArgumentException("Percentage completion cannot be negative");
        
        return new HabitProgress(id, habitId, date, percentageCompletion);
    }
}