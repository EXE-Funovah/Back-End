using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class LiveSessionProfile : Profile
{
    public LiveSessionProfile()
    {
        CreateMap<LiveSession, LiveSessionResponse>();
        CreateMap<LiveSessionCreateRequest, LiveSession>();
    }
}
