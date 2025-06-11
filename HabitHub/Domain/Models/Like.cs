namespace Domain.Models;

public class Like(
    Guid id, 
    Guid userId, 
    Guid postId
    )
{
    public Guid Id { get; } = id;
    public Guid UserId { get; } = userId;
    public Guid PostId { get; } = postId;

    public static Like Create(Guid id, Guid userId, Guid postId)
    {
        if (id == Guid.Empty)
            throw new ArgumentNullException(nameof(id));
        
        if (postId == Guid.Empty)
            throw new ArgumentNullException(nameof(postId));
        
        if (postId == Guid.Empty)
            throw new ArgumentNullException(nameof(postId));
        
        return new Like(id, userId, postId);
    }
}