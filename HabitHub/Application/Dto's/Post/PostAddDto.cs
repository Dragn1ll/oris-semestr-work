using Microsoft.AspNetCore.Http;

namespace Application.Dto_s.Post;

public class PostAddDto
{
    public Guid UserId { get; set; }
    public Guid HabitId { get; set; }
    public string Text { get; set; }
    public IEnumerable<IFormFile> MediaFiles { get; set; }
}