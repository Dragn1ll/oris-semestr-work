using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class HabitRepository(AppDbContext context, IMapper mapper) : 
    Repository<Habit, HabitEntity>(context, mapper), IHabitRepository
{
    
}