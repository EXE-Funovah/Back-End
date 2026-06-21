using AutoMapper;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Mappers;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // AvatarUrl (entity = S3 key) KHÔNG map thẳng — service tự set thành
        // presigned download URL sau khi map.
        CreateMap<User, UserResponse>()
            .ForMember(d => d.AvatarUrl, o => o.Ignore());
    }
}
