using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class SessionParticipantProfile : Profile
{
    public SessionParticipantProfile()
    {
        CreateMap<SessionParticipant, SessionParticipantResponse>();
        CreateMap<SessionParticipantCreateRequest, SessionParticipant>();
    }
}
