using System.ComponentModel.DataAnnotations;

namespace HabitHub.Requests.Comment;

public record AddCommentRequest(
    [Required] string Comment);