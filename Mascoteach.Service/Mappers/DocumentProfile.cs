using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class DocumentProfile : Profile
{
    public DocumentProfile()
    {
        // Ánh xạ từ Entity Document sang DTO DocumentResponse
        CreateMap<Document, DocumentResponse>()
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));
    }
}