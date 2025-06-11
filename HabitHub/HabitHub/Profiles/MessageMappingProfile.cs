using Application.Dto_s;
using Application.Dto_s.Message;
using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace HabitHub.Profiles;

public class MessageMappingProfile : Profile
{
    public MessageMappingProfile()
    {
        CreateMap<Message, MessageEntity>()
            .ForMember(dest => dest.Recipient, 
                opt => opt.Ignore())
            .ForMember(dest => dest.Sender, 
                opt => opt.Ignore());

        CreateMap<MessageEntity, Message>()
            .ConstructUsing(src => Message.Create(
                src.Id,
                src.RecipientId,
                src.SenderId,
                src.Text,
                src.DateTime
                )
            );

        CreateMap<MessageAddDto, Message>()
            .ConstructUsing(src => Message.Create(
                Guid.NewGuid(), 
                src.RecipientId,
                src.SenderId,
                src.Text,
                DateTime.UtcNow
                )
            );

        CreateMap<Message, IdDto>();
        
        CreateMap<Message, MessageInfoDto>();
    }
}