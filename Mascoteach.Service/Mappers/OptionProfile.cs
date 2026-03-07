using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class OptionProfile : Profile
{
    public OptionProfile()
    {
        CreateMap<Option, OptionResponse>();
        CreateMap<OptionCreateRequest, Option>();
    }
}
