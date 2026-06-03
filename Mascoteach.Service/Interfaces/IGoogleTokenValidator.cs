using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string credential);
}
