using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class MediaFileMappingProfile : Profile
{
    public MediaFileMappingProfile()
    {
        CreateMap<MediaFile, MediaFileEntity>()
            .ForMember(dest => dest.Post, 
                opt => opt.Ignore());

        CreateMap<MediaFileEntity, MediaFile>()
            .ConstructUsing(src => MediaFile.Create(
                src.Id,
                src.PostId,
                src.Extension,
                src.Type
            ));
    }
}