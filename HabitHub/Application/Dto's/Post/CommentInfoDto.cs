namespace Application.Dto_s.Post;

public class CommentInfoDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; }
    public DateTime DateTime { get; set; }
}