using Domain.Enums;

namespace Application.Extensions;

public static class MediaFileExtensions
{
    private static readonly Dictionary<string, MediaFileType> ExtensionToMediaType = 
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Изображения
        [".jpg"] = MediaFileType.Image,
        [".jpeg"] = MediaFileType.Image,
        [".png"] = MediaFileType.Image,
        [".bmp"] = MediaFileType.Image,
        [".webp"] = MediaFileType.Image,

        // Гифки
        [".gif"] = MediaFileType.Gif,

        // Видео
        [".mp4"] = MediaFileType.Video,
        [".mov"] = MediaFileType.Video,
        [".avi"] = MediaFileType.Video,
        [".mkv"] = MediaFileType.Video,
        [".webm"] = MediaFileType.Video,

        // Аудио
        [".mp3"] = MediaFileType.Audio,
        [".wav"] = MediaFileType.Audio,
        [".aac"] = MediaFileType.Audio,
        [".flac"] = MediaFileType.Audio,
        [".ogg"] = MediaFileType.Audio
    };

    public static bool TryGetMediaType(string extension, out MediaFileType type)
    {
        return ExtensionToMediaType.TryGetValue(extension, out type);
    }
}
