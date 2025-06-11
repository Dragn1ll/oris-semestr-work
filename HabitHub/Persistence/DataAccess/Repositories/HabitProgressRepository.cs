using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class HabitProgressRepository(AppDbContext context, IMapper mapper) 
    : Repository<HabitProgress, HabitProgressEntity>(context, mapper), IHabitProgressRepository
{
    
}