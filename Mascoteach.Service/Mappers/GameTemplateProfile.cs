using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class GameTemplateProfile : Profile
{
    public GameTemplateProfile()
    {
        CreateMap<GameTemplate, GameTemplateResponse>();
        CreateMap<GameTemplateCreateRequest, GameTemplate>();
    }
}
