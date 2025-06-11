using Domain.Enums;

namespace Domain.Models;

public class Message(
    Guid id,
    Guid recipientId,
    Guid senderId,
    string text,
    DateTime dateTime
    )
{
    public Guid Id { get; } = id;
    public Guid RecipientId { get; } = recipientId;
    public Guid SenderId { get; } = senderId;
    public string Text { get; } = text;
    public DateTime DateTime { get; } = dateTime;

    public static Message Create(Guid id, Guid recipientId, Guid senderId, string text, DateTime dateTime)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (recipientId == Guid.Empty)
            throw new ArgumentException("Chat Id cannot be empty");
        
        if (senderId == Guid.Empty)
            throw new ArgumentException("User Id cannot be empty");
        
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be empty");
        
        return new Message(id, recipientId, senderId, text, dateTime);
    }
}