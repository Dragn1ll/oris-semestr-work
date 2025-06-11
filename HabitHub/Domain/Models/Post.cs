namespace Domain.Models;

public class Post(
    Guid id, 
    Guid userId, 
    Guid habitId, 
    string text,
    DateTime dateTime
    )
{
    public Guid Id { get; } = id;
    public Guid UserId { get; } = userId;
    public Guid HabitId { get; } = habitId;
    public string Text { get; } = text;
    public DateTime DateTime { get; } = dateTime;

    public static Post Create(Guid id, Guid userId, Guid habitId, string text, DateTime dateTime)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (userId == Guid.Empty)
            throw new ArgumentException("Physical activity type cannot be empty");
        
        if (habitId == Guid.Empty)
            throw new ArgumentException("HabitId cannot be empty");
        
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty");
        
        return new Post(id, userId, habitId, text, dateTime);
    }
}