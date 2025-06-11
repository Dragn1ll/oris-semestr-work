using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum MediaFileType
{
    [Display(Name = "Изображение")]
    Image = 1,
    
    [Display(Name = "Гифка")]
    Gif,
    
    [Display(Name = "Видео")]
    Video,
    
    [Display(Name = "Аудио")]
    Audio
}