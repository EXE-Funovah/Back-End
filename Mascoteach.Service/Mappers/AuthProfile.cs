using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class AuthProfile : Profile
{
    public AuthProfile()
    {
        CreateMap<User, AuthResponse>()
            .ForMember(dest => dest.Token, opt => opt.Ignore());
    }
}
