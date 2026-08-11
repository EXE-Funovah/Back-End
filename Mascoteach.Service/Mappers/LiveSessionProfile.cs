using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class LiveSessionProfile : Profile
{
    public LiveSessionProfile()
    {
        CreateMap<LiveSession, LiveSessionResponse>()
            .ForMember(destination => destination.QuizTitle,
                options => options.MapFrom(source => source.Quiz == null ? null : source.Quiz.Title))
            .ForMember(destination => destination.ParticipantCount,
                options => options.MapFrom(source => source.SessionParticipants.Count(participant => !participant.IsDeleted)));
        CreateMap<LiveSessionCreateRequest, LiveSession>();
    }
}
