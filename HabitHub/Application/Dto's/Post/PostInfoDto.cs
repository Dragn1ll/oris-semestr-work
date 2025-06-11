using Application.Utils;
using Domain.Models;

namespace Application.Dto_s.Post;

public class PostInfoDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid HabitId { get; set; }
    public string Text { get; set; }
    public DateTime DateTime { get; set; }
    public IEnumerable<string> MediaFilesUrl { get; set; }
    public uint LikesCount { get; set; }
    public bool DidUserLiked { get; set; }
    public ulong CommentsCount { get; set; }
}