using System.ComponentModel.DataAnnotations;

namespace HabitHub.Requests.Google;

public record FitAnalyzeRequest(
    [Required] Guid HabitId,
    [Required] DateTime FromDate,
    [Required] DateTime ToDate
    );