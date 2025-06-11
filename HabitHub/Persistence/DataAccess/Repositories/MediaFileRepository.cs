using Application.Interfaces.Repositories;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Persistence.DataAccess.Repositories;

public class MediaFileRepository(AppDbContext context, IMapper mapper) : 
    Repository<MediaFile, MediaFileEntity>(context, mapper), IMediaFileRepository
{
    
}