using Domain.Enums;

namespace Domain.Models;

public class ActivityData
{
    public PhysicalActivityType ActivityType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Calories { get; set; }
    public int Steps { get; set; }
    public double Distance { get; set; }
}