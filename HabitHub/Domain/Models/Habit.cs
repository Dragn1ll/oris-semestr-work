using Domain.Enums;

namespace Domain.Models;

public class Habit(
    Guid id,
    Guid userId,
    HabitType habitType,
    PhysicalActivityType? physicalActivityType,
    string goal,
    bool isActive
    )
{
    public Guid Id { get; } = id;
    public Guid UserId { get; } = userId;
    public HabitType Type { get; } = habitType;
    public PhysicalActivityType? PhysicalActivityType { get; } = physicalActivityType;
    public string Goal { get; } = goal;
    public bool IsActive { get; } = isActive;

    public static Habit Create(Guid id, Guid userId, HabitType habitType, PhysicalActivityType? physicalActivityType,
        string goal, bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Habit id cannot be empty");
        
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty");
        
        if (habitType == default)
            throw new ArgumentException("Habit type cannot be empty");
        
        if (habitType == HabitType.PhysicalActivity && physicalActivityType == null)
            throw new ArgumentException("Physical activity type cannot be empty");
        
        if (string.IsNullOrWhiteSpace(goal))
            throw new ArgumentException("Goal cannot be empty");
        
        return new Habit(id, userId, habitType, physicalActivityType, goal, isActive);
    }
}