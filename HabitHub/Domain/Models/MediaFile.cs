using Domain.Enums;

namespace Domain.Models;

public class MediaFile(
    Guid id,
    Guid postId,
    string extension,
    MediaFileType fileType
    )
{
    public Guid Id { get; } = id;
    public Guid PostId { get; } = postId;
    public string Extension { get; } = extension;
    public MediaFileType Type { get; } = fileType;

    public static MediaFile Create(Guid id, Guid postId, string extension, MediaFileType fileType)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty");
        
        if (postId == Guid.Empty)
            throw new ArgumentException("PostId or UserId cannot be empty");
        
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty");
        
        if (fileType == default)
            throw new ArgumentException("File type cannot be empty");
        
        return new MediaFile(id, postId, extension, fileType);
    }
}