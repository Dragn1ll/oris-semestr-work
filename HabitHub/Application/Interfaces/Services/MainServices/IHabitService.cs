using Application.Dto_s;
using Application.Dto_s.Habit;
using Application.Utils;

namespace Application.Interfaces.Services.MainServices;

public interface IHabitService
{
    Task<Result<IdDto>> AddAsync(HabitAddDto habitAddDto);
    Task<Result<IdDto>> AddProgressAsync(Guid userId, HabitProgressAddDto habitProgressAddDto);
    Task<Result<HabitInfoDto>> GetByIdAsync(Guid habitId);
    Task<Result<IEnumerable<HabitInfoDto>>> GetAllByUserIdAsync(Guid userId);
    Task<Result<IEnumerable<HabitProgressInfoDto>>> GetAllProgressByHabitIdAsync(Guid habitId);
    Task<Result> UpdateByIdAsync(Guid userId, HabitInfoDto habitInfoDto);
    Task<Result> DeleteByIdAsync(Guid userId, Guid habitId);
}