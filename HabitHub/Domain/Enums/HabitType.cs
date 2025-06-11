using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum HabitType
{
    [Display(Name = "Физическая активность")]
    PhysicalActivity = 1,
    
    [Display(Name = "Здоровое питание")]
    HealthyEating,
    
    [Display(Name = "Умственные привычки")]
    Mental,
    
    [Display(Name = "Продуктивность и организация")]
    Productivity,
    
    [Display(Name = "Финансовые привычки")]
    Financial,
    
    [Display(Name = "Социальные привычки")]
    Social,
    
    [Display(Name = "Духовные практики")]
    Spiritual,
    
    [Display(Name = "Гигиена и уход за собой")]
    Hygiene,
    
    [Display(Name = "Творческие привычки")]
    Creative,
    
    [Display(Name = "Экологические привычки")]
    Environmental,
    
    [Display(Name = "Привычки сна")]
    Sleep,
    
    [Display(Name = "Вредные привычки")]
    BadHabit,
    
    [Display(Name = "Другие привычки")]
    Other
}