using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IS3Service
{
    Task<PresignedUrlResponse> GeneratePresignedUploadUrlAsync(string fileName, string contentType);

    /// <summary>
    /// Presign upload cho ảnh đại diện — cho phép contentType ảnh (image/jpeg,
    /// image/png) và key prefix "avatars/", khác document (ép .zip).
    /// </summary>
    Task<PresignedUrlResponse> GeneratePresignedAvatarUploadUrlAsync(string fileName, string contentType);

    Task<string> GeneratePresignedDownloadUrlAsync(string s3Key);
}
