using Domain.Entities;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IHabitRepository : IRepository<Habit, HabitEntity>
{
    
}