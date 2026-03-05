using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class QuizProfile : Profile
{
    public QuizProfile()
    {
        CreateMap<Quiz, QuizResponse>();
        CreateMap<QuizCreateRequest, Quiz>();
    }
}
