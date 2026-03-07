using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class QuestionProfile : Profile
{
    public QuestionProfile()
    {
        CreateMap<Question, QuestionResponse>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options));
        CreateMap<QuestionCreateRequest, Question>()
            .ForMember(dest => dest.Options, opt => opt.Ignore());
    }
}
