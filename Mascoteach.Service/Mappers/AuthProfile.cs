using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mascoteach.Service.Mappers
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            // map user entity to auth response
            CreateMap<User, AuthResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Token, opt => opt.Ignore());
        }
    }
}
