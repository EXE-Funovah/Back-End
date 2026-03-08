using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class DocumentProfile : Profile
{
    public DocumentProfile()
    {
        CreateMap<Document, DocumentResponse>()
            .ForMember(dest => dest.S3Key, opt => opt.MapFrom(src => src.FileUrl))
            .ForMember(dest => dest.PresignedUrl, opt => opt.Ignore());
    }
}