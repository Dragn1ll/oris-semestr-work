namespace Domain.Models;

public class Comment(
    Guid id, 
    Guid userId, 
    Guid postId, 
    string text, 
    DateTime dateTime
    )
{
    public Guid Id { get; } = id;
    public Guid UserId { get; } = userId;
    public Guid PostId { get; } = postId;
    public string Text { get; } = text;
    public DateTime DateTime { get; } = dateTime;

    public static Comment Create(Guid id, Guid userId, Guid postId, string text, DateTime dateTime)
    {
        if (id == Guid.Empty)
            throw new ArgumentNullException(nameof(id));
        
        if (userId == Guid.Empty)
            throw new ArgumentNullException(nameof(userId));
        
        if (postId == Guid.Empty)
            throw new ArgumentNullException(nameof(postId));
        
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(null, nameof(text));
        
        if (dateTime == DateTime.MinValue)
            throw new ArgumentNullException(nameof(dateTime));
        
        return new Comment(id, userId, postId, text, dateTime);
    }
}