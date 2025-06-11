using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum PhysicalActivityType
{
    [Display(Name = "Ходьба")]
    Walking = 1,
    
    [Display(Name = "Бег")]
    Running,
    
    [Display(Name = "Велоспорт")]
    Cycling,
    
    [Display(Name = "Плавание")]
    Swimming,
    
    [Display(Name = "Лыжи")]
    Skiing,
    
    [Display(Name = "Сноуборд")]
    Snowboarding,
    
    [Display(Name = "Йога")]
    Yoga,
    
    [Display(Name = "Другая активность")]
    Other
}