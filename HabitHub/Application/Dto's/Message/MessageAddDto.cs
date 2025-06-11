namespace Application.Dto_s.Message;

public class MessageAddDto
{
    public Guid RecipientId { get; set; }
    public Guid SenderId { get; set; }
    public string Text { get; set; }
}