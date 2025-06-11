namespace Application.Dto_s.Message;

public class MessageInfoDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public string Text { get; set; }
    public DateTime DateTime { get; set; }
}