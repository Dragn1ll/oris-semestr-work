using Application.Utils;
using Domain.Models;

namespace Application.Interfaces.Services.HelperServices;

public interface IGoogleFitService
{
    Task<Result<ICollection<ActivityData>>> GetActivityDataAsync(Guid userId, DateTime startDate, DateTime endDate);
}